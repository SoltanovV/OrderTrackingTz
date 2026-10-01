using OrderTracking.Application.Models.Response;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Utilities;
using OrderTracking.Domain.Models.Entity;
using OrderTracking.Domain.Models.Enums;
using OrderTracking.Infrastructure.Persistence;
using OrderTracking.Infrastructure.Services;
using OrderTracking.Infrastructure.Services.Background;
using OrderTracking.Infrastructure.Utilities;
using Xunit;

namespace OrderTracking.Tests;

public sealed class EventHubTests
{
    [Fact]
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
        // Queue overflow terminates only this client's stream, forcing snapshot resync.
        for (var i = 0; i < 129; i++) hub.Publish(message);
        Assert.False(all.Queue.Writer.TryWrite(message));
        Assert.True(unrelated.Queue.Writer.TryWrite(message));
    }
}
