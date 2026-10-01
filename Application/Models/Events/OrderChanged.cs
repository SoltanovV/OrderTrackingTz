using OrderTracking.Application.Models.Response;

namespace OrderTracking.Application.Models.Events;

/// <summary>Событие создания заказа или изменения его статуса.</summary>
public sealed record OrderChanged
{
    /// <summary>Уникальный идентификатор события для распознавания повторной доставки.</summary>
    public Guid EventId { get; init; }

    /// <summary>Снимок заказа на момент возникновения события.</summary>
    public OrderDto Order { get; init; }

    /// <summary>Создаёт модель «Событие создания заказа или изменения его статуса».</summary>
    /// <param name="eventId">Уникальный идентификатор события для распознавания повторной доставки.</param>
    /// <param name="order">Снимок заказа на момент возникновения события.</param>
    public OrderChanged(Guid eventId, OrderDto order)
    {
        EventId = eventId;
        Order = order;
    }
}
