using Microsoft.AspNetCore.Mvc;
using OrderTracking.Application.Models.Response;
using OrderTracking.Application.Services.Interface;

namespace OrderTracking.Api.Controllers;

/// <summary>Проверки работоспособности процесса и готовности внешних зависимостей.</summary>
[ApiController]
[Route("health")]
public sealed class HealthController(IReadinessService readiness) : ControllerBase
{
    /// <summary>Подтверждает что процесс приложения принимает HTTP-запросы.</summary>
    [HttpGet("live")]
    public ActionResult<LivenessResponse> Live() => Ok(new LivenessResponse());

    /// <summary>Возвращает готовность приложения с учётом состояния базы данных и брокера.</summary>
    [HttpGet]
    public async Task<ActionResult<ReadinessResponse>> Ready(CancellationToken ct)
    {
        var response = await readiness.CheckAsync(ct);
        return StatusCode(response.Database && response.Broker ? 200 : 503, response);
    }
}
