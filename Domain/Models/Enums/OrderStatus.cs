namespace OrderTracking.Domain.Models.Enums;

/// <summary>
/// Статус заказа на одном из этапов его жизненного цикла.
/// </summary>
public enum OrderStatus
{
    /// <summary>Заказ создан и ожидает отправки.</summary>
    Created,

    /// <summary>Заказ отправлен получателю.</summary>
    Shipped,

    /// <summary>Заказ доставлен получателю.</summary>
    Delivered,

    /// <summary>Заказ отменён без возможности возобновления.</summary>
    Cancelled
}
