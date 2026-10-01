using System.Threading.Channels;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Services.Interface;

namespace OrderTracking.Infrastructure.Services;

/// <summary>Независимая подписка с ограниченным буфером событий и очисткой при отключении клиента.</summary>
public sealed class OrderEventSubscription(Guid id, Guid? orderId, Action<Guid> unsubscribe) : IOrderEventSubscription
{
    /// <summary>Уникальный идентификатор подписки.</summary>
    public Guid Id { get; } = id;

    /// <summary>Идентификатор отслеживаемого заказа; null означает подписку на все заказы.</summary>
    public Guid? OrderId { get; } = orderId;

    /// <summary>Ограниченный буфер доставки событий одному подписчику.</summary>
    public Channel<OrderChanged> Queue { get; } = Channel.CreateBounded<OrderChanged>(new BoundedChannelOptions(128)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.Wait
    });

    /// <summary>Доступ к событиям подписки только для чтения.</summary>
    public ChannelReader<OrderChanged> Reader => Queue.Reader;

    /// <summary>Удаляет подписку из реестра и завершает поток событий.</summary>
    public void Dispose()
    {
        unsubscribe(Id);
        Queue.Writer.TryComplete();
    }
}
