using OrderTracking.Domain.Models.Entity;

namespace OrderTracking.Application.Models.Query;

/// <summary>Результат чтения страницы заказов из хранилища.</summary>
public sealed record OrderSearchResult
{
    /// <summary>Заказы текущей страницы вместе с историей.</summary>
    public IReadOnlyList<Order> Items { get; init; }

    /// <summary>Общее количество найденных заказов.</summary>
    public int Total { get; init; }

    /// <summary>Создаёт модель «Результат чтения страницы заказов из хранилища».</summary>
    /// <param name="items">Заказы текущей страницы вместе с историей.</param>
    /// <param name="total">Общее количество найденных заказов.</param>
    public OrderSearchResult(IReadOnlyList<Order> items, int total)
    {
        Items = items;
        Total = total;
    }
}
