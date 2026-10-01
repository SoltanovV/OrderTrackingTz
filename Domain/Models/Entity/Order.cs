using OrderTracking.Domain.Models.Enums;

namespace OrderTracking.Domain.Models.Entity;

/// <summary>
/// Заказ с текущим статусом, историей изменений и версией для контроля конкурентного доступа.
/// </summary>
public sealed class Order
{
    private readonly List<OrderStatusChange> history = [];

    /// <summary>Уникальный идентификатор заказа.</summary>
    public Guid Id { get; private set; }

    /// <summary>Уникальный номер заказа, отображаемый пользователю.</summary>
    public string OrderNumber { get; private set; } = "";

    /// <summary>Описание содержимого заказа.</summary>
    public string Description { get; private set; } = "";

    /// <summary>Текущий статус заказа.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Дата и время создания заказа в UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Дата и время последнего изменения статуса в UTC.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Версия заказа, увеличиваемая при каждом изменении статуса.</summary>
    public int Version { get; private set; }

    /// <summary>История статусов заказа, включая первоначальное создание.</summary>
    public IReadOnlyCollection<OrderStatusChange> History => history.AsReadOnly();

    private Order() { }

    /// <summary>Создаёт заказ в начальном статусе и добавляет первую запись истории.</summary>
    /// <param name="orderNumber">Нормализованный уникальный номер заказа.</param>
    /// <param name="description">Описание заказа без пробелов по краям.</param>
    /// <param name="now">Момент создания заказа в UTC.</param>
    /// <returns>Созданный заказ с первой версией.</returns>
    public static Order Create(string orderNumber, string description, DateTime now)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            Description = description,
            Status = OrderStatus.Created,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };
        order.history.Add(new OrderStatusChange(order.Id, order.Status, now, order.Version));
        return order;
    }

    /// <summary>Меняет статус с проверкой допустимого перехода и сохраняет изменение в истории.</summary>
    /// <param name="next">Новый статус заказа.</param>
    /// <param name="now">Момент изменения статуса в UTC.</param>
    /// <returns>Признак изменения статуса; для повторной установки текущего статуса — false.</returns>
    /// <exception cref="InvalidOperationException">Переход в указанный статус недопустим.</exception>
    public bool ChangeStatus(OrderStatus next, DateTime now)
    {
        if (next == Status) return false;

        var allowed = Status switch
        {
            OrderStatus.Created => next is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => next is OrderStatus.Delivered or OrderStatus.Cancelled,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException("Недопустимый переход статуса заказа.");

        Status = next;
        UpdatedAt = now;
        Version++;
        history.Add(new OrderStatusChange(Id, next, now, Version));
        return true;
    }
}
