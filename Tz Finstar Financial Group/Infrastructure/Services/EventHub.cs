using System.Collections.Concurrent;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Services.Interface;

namespace OrderTracking.Infrastructure.Services;

/// <summary>Распределяет события из брокера между независимыми подписками клиентов.</summary>
public sealed class EventHub : IOrderEventStream, IOrderEventPublisher
{
    private readonly ConcurrentDictionary<Guid, OrderEventSubscription> subscribers = new();

    /// <summary>Создаёт подписку на один заказ или на все заказы.</summary>
    public OrderEventSubscription Subscribe(Guid? orderId)
    {
        var subscription = new OrderEventSubscription(Guid.NewGuid(), orderId, Unsubscribe);
        subscribers.TryAdd(subscription.Id, subscription);
        return subscription;
    }

    IOrderEventSubscription IOrderEventStream.Subscribe(Guid? orderId) => Subscribe(orderId);

    /// <summary>Доставляет событие подписчикам; переполненная подписка завершает поток для восстановления данных.</summary>
    public void Publish(OrderChanged message)
    {
        foreach (var subscription in subscribers.Values)
        {
            if (subscription.OrderId is not null && subscription.OrderId != message.Order.Id) continue;

            // Медленный клиент переподключится и получит актуальный снимок заказа.
            if (!subscription.Queue.Writer.TryWrite(message)) subscription.Queue.Writer.TryComplete();
        }
    }

    private void Unsubscribe(Guid id) => subscribers.TryRemove(id, out _);
}
