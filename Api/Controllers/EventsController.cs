using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Services.Interface;

namespace OrderTracking.Api.Controllers;

/// <summary>Передаёт клиентам изменения заказов по протоколу Server-Sent Events.</summary>
[ApiController]
[Route("api/events")]
public sealed class EventsController(IOrderEventStream events, IBrokerStatus broker, IOptions<JsonOptions> json) : ControllerBase
{
    /// <summary>Открывает поток изменений одного заказа или всех заказов с контрольными событиями соединения.</summary>
    [HttpGet]
    public async Task Subscribe(CancellationToken ct, [FromQuery] Guid? orderId = null)
    {
        using var subscription = events.Subscribe(orderId);
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        try
        {
            await Response.WriteAsync("retry: 3000\n", ct);
            await WriteAsync("ready", new StreamStatus { BrokerConnected = broker.Connected }, ct);
            while (!ct.IsCancellationRequested)
            {
                using var heartbeat = CancellationTokenSource.CreateLinkedTokenSource(ct);
                heartbeat.CancelAfter(TimeSpan.FromSeconds(1));
                try
                {
                    if (!await subscription.Reader.WaitToReadAsync(heartbeat.Token)) break;
                    while (subscription.Reader.TryRead(out var message))
                        await WriteAsync("order-changed", message, ct);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    await WriteAsync("heartbeat", new StreamStatus { BrokerConnected = broker.Connected }, ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private async Task WriteAsync<T>(string eventName, T data, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(data, json.Value.JsonSerializerOptions);
        await Response.WriteAsync($"event: {eventName}\ndata: {payload}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
