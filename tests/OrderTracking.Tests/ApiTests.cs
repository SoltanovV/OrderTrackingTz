using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderTracking.Application.Models.Response;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Utilities;
using OrderTracking.Domain.Models.Entity;
using OrderTracking.Domain.Models.Enums;
using OrderTracking.Infrastructure.Persistence;
using OrderTracking.Infrastructure.Services;
using OrderTracking.Infrastructure.Services.Background;
using OrderTracking.Infrastructure.Utilities;
using Xunit;

namespace OrderTracking.Tests;

public sealed class ApiTests(TestHost host) : IClassFixture<TestHost>
{
    private readonly HttpClient client = host.CreateClient();
    private static readonly JsonSerializerOptions Json = EventJson.Options;

    private async Task<OrderDto> Create(string? number = null)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { orderNumber = number ?? Guid.NewGuid().ToString(), description = "  Laptop and accessories  " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderDto>(Json))!;
        Assert.EndsWith($"/api/orders/{order.Id}", response.Headers.Location!.ToString());
        return order;
    }

    [Fact]
    public async Task CreateGetAndChangeStatusPersistHistoryAndOutboxAtomically()
    {
        var order = await Create("  api-lifecycle  ");
        Assert.Equal("API-LIFECYCLE", order.OrderNumber);
        Assert.Equal("Laptop and accessories", order.Description);
        Assert.Equal(OrderStatus.Created, order.Status);
        var response = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Shipped", version = 1 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}", Json))!;
        Assert.Equal(OrderStatus.Shipped, updated.Status);
        Assert.Equal(2, updated.Version);
        Assert.Equal(2, updated.History.Count);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var events = (await db.OutboxMessages.AsNoTracking().ToListAsync()).Select(x => JsonSerializer.Deserialize<OrderChanged>(x.Payload, Json)!)
            .Where(x => x.Order.Id == order.Id).ToList();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, x => x.Order.Status == OrderStatus.Shipped && x.Order.Version == 2);
    }

    [Fact]
    public async Task DuplicateOrderNumberIsCaseInsensitive()
    {
        await Create("Duplicate-Number");
        var response = await client.PostAsJsonAsync("/api/orders", new { orderNumber = " duplicate-number ", description = "Test" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Test")]
    [InlineData("  ", "Test")]
    [InlineData("A", "")]
    [InlineData("A", "   ")]
    public async Task RequiredFieldsRejectWhitespace(string number, string description)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { orderNumber = number, description });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LengthLimitsAreEnforced()
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { orderNumber = new string('x', 65), description = new string('x', 2001) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StaleVersionAndInvalidTransitionDoNotWriteHistoryOrEvents()
    {
        var order = await Create();
        var skipped = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Delivered", version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, skipped.StatusCode);
        var changed = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Shipped", version = 1 });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var stale = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Cancelled", version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var same = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Shipped", version = 2 });
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        var latest = (await client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}", Json))!;
        Assert.Equal(2, latest.History.Count);
        Assert.Equal(OrderStatus.Shipped, latest.Status);
    }

    [Theory]
    [InlineData("{\"status\":\"Unknown\",\"version\":1}")]
    [InlineData("{\"status\":88,\"version\":1}")]
    [InlineData("{\"status\":\"Shipped\",\"version\":0}")]
    public async Task InvalidStatusPayloadReturns400(string body)
    {
        var order = await Create();
        var response = await client.PatchAsync($"/api/orders/{order.Id}/status", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchFilterPagingAndTrackedIdsAreApplied()
    {
        var a = await Create("SEARCH-ONE");
        var b = await Create("SEARCH-TWO");
        await client.PatchAsJsonAsync($"/api/orders/{b.Id}/status", new { status = "Shipped", version = 1 });
        var page = (await client.GetFromJsonAsync<OrderPage>("/api/orders?search=search-&pageSize=1", Json))!;
        Assert.Equal(2, page.Total); Assert.Single(page.Items);
        var filtered = (await client.GetFromJsonAsync<OrderPage>("/api/orders?search=search-&status=Shipped", Json))!;
        Assert.Equal(b.Id, Assert.Single(filtered.Items).Id);
        var tracked = (await client.GetFromJsonAsync<OrderPage>($"/api/orders?ids={a.Id}", Json))!;
        Assert.Equal(a.Id, Assert.Single(tracked.Items).Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?status=99")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?pageSize=101")).StatusCode);
    }

    [Fact]
    public async Task MissingOrderReturns404AndBrokerOutageIsVisibleInHealth()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/orders/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task SseStreamsReadyThenOrderEvent()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var response = await client.GetAsync("/api/events", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        Assert.Equal("retry: 3000", await reader.ReadLineAsync(timeout.Token));
        Assert.Equal("event: ready", await reader.ReadLineAsync(timeout.Token));
        Assert.Contains("brokerConnected", await reader.ReadLineAsync(timeout.Token));
        Assert.Equal("", await reader.ReadLineAsync(timeout.Token));
        var order = await Create();
        host.Services.GetRequiredService<EventHub>().Publish(new OrderChanged(Guid.NewGuid(), order));
        Assert.Equal("event: order-changed", await reader.ReadLineAsync(timeout.Token));
        Assert.Contains(order.Id.ToString(), await reader.ReadLineAsync(timeout.Token));
    }
}
