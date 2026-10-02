using OrderTracking.Domain.Models.Entity;
using OrderTracking.Domain.Models.Enums;
using Xunit;

namespace OrderTracking.Tests;

/// <summary>Правила жизненного цикла заказа без HTTP и базы данных.</summary>
public sealed class OrderTests
{
    private static readonly DateTime Now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Проверяет переходы «Создан → Отправлен → Доставлен», порядок истории, версии и даты.</summary>
    [Fact(DisplayName = "Жизненный цикл заказа и история статусов")]
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

    /// <summary>Проверяет запрет возвращения в работу и доставки после отмены до или после отправки.</summary>
    [Theory(DisplayName = "Отменённый заказ нельзя вернуть в работу")]
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
}
