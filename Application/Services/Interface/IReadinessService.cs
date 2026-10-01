using OrderTracking.Application.Models.Response;

namespace OrderTracking.Application.Services.Interface;

/// <summary>Проверка доступности внешних зависимостей приложения.</summary>
public interface IReadinessService
{
    /// <summary>Возвращает состояние базы данных и подключения к брокеру.</summary>
    Task<ReadinessResponse> CheckAsync(CancellationToken ct);
}
