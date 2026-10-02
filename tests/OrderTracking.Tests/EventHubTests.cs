using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Utilities;
using OrderTracking.Domain.Models.Entity;
using OrderTracking.Infrastructure.Services;
using Xunit;

namespace OrderTracking.Tests;

/// <summary>Доставка событий подписчикам и защита от переполнения их буферов.</summary>
public sealed class EventHubTests
{
    /// <summary>Проверяет выбор подписчиков по заказу и закрытие переполненного буфера без отключения остальных клиентов.</summary>
    [Fact(DisplayName = "Фильтрация событий и изоляция медленных подписчиков")]
    public void BroadcastFiltersSubscriptionsAndIsolatesSlowClients()
    {
        var hub = new EventHub();
        var order = OrderMapper.ToDto(Order.Create("TEST", "Test", DateTime.UtcNow));
        using var all = hub.Subscribe(null);
        using var matching = hub.Subscribe(order.Id);
        using var unrelated = hub.Subscribe(Guid.NewGuid());
        var message = new OrderChanged(Guid.NewGuid(), order);
        hub.Publish(message);
        Assert.True(all.Queue.Reader.TryRead(out _));
        Assert.True(matching.Queue.Reader.TryRead(out _));
        Assert.False(unrelated.Queue.Reader.TryRead(out _));
        for (var i = 0; i < 129; i++) hub.Publish(message);
        Assert.False(all.Queue.Writer.TryWrite(message));
        Assert.True(unrelated.Queue.Writer.TryWrite(message));
    }
}
