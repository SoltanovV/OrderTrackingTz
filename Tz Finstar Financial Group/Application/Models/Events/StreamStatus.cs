namespace OrderTracking.Application.Models.Events;

/// <summary>Состояние доставки уведомлений, передаваемое при подключении и в контрольных событиях SSE.</summary>
public sealed class StreamStatus
{
    /// <summary>Признак доступности брокера для получения обновлений заказа.</summary>
    public bool BrokerConnected { get; init; }
}
