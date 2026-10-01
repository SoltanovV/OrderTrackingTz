using OrderTracking.Domain.Models.Enums;

namespace OrderTracking.Domain.Models.Entity;

/// <summary>Запись истории о переходе заказа в определённый статус.</summary>
public sealed class OrderStatusChange
{
    /// <summary>Уникальный идентификатор записи истории.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Идентификатор заказа, к которому относится изменение.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>Статус заказа после изменения.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Дата и время изменения статуса в UTC.</summary>
    public DateTime ChangedAt { get; private set; }

    /// <summary>Версия заказа после изменения статуса.</summary>
    public int Version { get; private set; }

    private OrderStatusChange() { }

    /// <summary>Создаёт запись истории изменения статуса заказа.</summary>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <param name="status">Новый статус заказа.</param>
    /// <param name="changedAt">Момент изменения в UTC.</param>
    /// <param name="version">Версия заказа после изменения.</param>
    public OrderStatusChange(Guid orderId, OrderStatus status, DateTime changedAt, int version)
        => (OrderId, Status, ChangedAt, Version) = (orderId, status, changedAt, version);
}
