using OrderTracking.Application.Models.Events;

namespace OrderTracking.Application.Services.Interface;

/// <summary>Доставка подтверждённых брокером событий локальным подписчикам.</summary>
public interface IOrderEventPublisher
{
    /// <summary>Передаёт событие подходящим подписчикам.</summary>
    void Publish(OrderChanged message);
}
