using OrderTracking.Domain.Models.Entity;
using OrderTracking.Domain.Models.Enums;
using Xunit;

namespace OrderTracking.Tests;

public sealed class OrderTests
{
    private static readonly DateTime Now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FullLifecycleRecordsOrderedHistoryAndDates()
    {
        var order = Order.Create("A-1", "Test order", Now);
        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.Equal(1, order.Version);
        order.ChangeStatus(OrderStatus.Shipped, Now.AddHours(1));
        order.ChangeStatus(OrderStatus.Delivered, Now.AddHours(2));
        Assert.Equal(Now, order.CreatedAt);
        Assert.Equal(Now.AddHours(2), order.UpdatedAt);
        Assert.Equal(new[] { OrderStatus.Created, OrderStatus.Shipped, OrderStatus.Delivered }, order.History.Select(x => x.Status));
        Assert.Equal(new[] { 1, 2, 3 }, order.History.Select(x => x.Version));
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData((OrderStatus)99)]
    public void CannotSkipShippingOrUseUnknownStatus(OrderStatus next)
    {
        var order = Order.Create("A-1", "Test", Now);
        Assert.Throws<InvalidOperationException>(() => order.ChangeStatus(next, Now));
        Assert.Equal(1, order.Version);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationIsTerminal(bool shipped)
    {
        var order = Order.Create("A-1", "Test", Now);
        if (shipped) order.ChangeStatus(OrderStatus.Shipped, Now);
        order.ChangeStatus(OrderStatus.Cancelled, Now);
        Assert.Throws<InvalidOperationException>(() => order.ChangeStatus(OrderStatus.Created, Now));
        Assert.Throws<InvalidOperationException>(() => order.ChangeStatus(OrderStatus.Delivered, Now));
    }

    [Fact]
    public void SameStatusDoesNotAddHistoryOrChangeTimestamp()
    {
        var order = Order.Create("A-1", "Test", Now);
        Assert.False(order.ChangeStatus(OrderStatus.Created, Now.AddDays(1)));
        Assert.Single(order.History);
        Assert.Equal(Now, order.UpdatedAt);
    }
}
