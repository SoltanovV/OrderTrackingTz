using Microsoft.EntityFrameworkCore;
using OrderTracking.Application.Models.Response;
using OrderTracking.Application.Services.Interface;
using OrderTracking.Infrastructure.Persistence;

namespace OrderTracking.Infrastructure.Services;

/// <summary>Проверяет доступность PostgreSQL и готовность подписки RabbitMQ.</summary>
public sealed class ReadinessService(OrdersDbContext db, IBrokerStatus broker) : IReadinessService
{
    /// <summary>Проверяет соединение с базой данных и считывает состояние брокера.</summary>
    public async Task<ReadinessResponse> CheckAsync(CancellationToken ct) => new()
    {
        Database = await db.Database.CanConnectAsync(ct),
        Broker = broker.Connected
    };
}
