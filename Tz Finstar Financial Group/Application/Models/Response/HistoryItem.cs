using OrderTracking.Domain.Models.Enums;

namespace OrderTracking.Application.Models.Response;

/// <summary>Сведения об одном изменении статуса заказа.</summary>
public sealed record HistoryItem
{
    /// <summary>Статус заказа после изменения.</summary>
    public OrderStatus Status { get; init; }

    /// <summary>Дата и время изменения в UTC.</summary>
    public DateTime ChangedAt { get; init; }

    /// <summary>Версия заказа после изменения.</summary>
    public int Version { get; init; }

    /// <summary>Создаёт модель «Сведения об одном изменении статуса заказа».</summary>
    /// <param name="status">Статус заказа после изменения.</param>
    /// <param name="changedAt">Дата и время изменения в UTC.</param>
    /// <param name="version">Версия заказа после изменения.</param>
    public HistoryItem(OrderStatus status, DateTime changedAt, int version)
    {
        Status = status;
        ChangedAt = changedAt;
        Version = version;
    }
}
