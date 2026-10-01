namespace OrderTracking.Application.Models.Response;

/// <summary>Страница заказов с общим количеством результатов.</summary>
public sealed record OrderPage
{
    /// <summary>Заказы текущей страницы.</summary>
    public IReadOnlyList<OrderDto> Items { get; init; }

    /// <summary>Количество заказов, соответствующих условиям поиска.</summary>
    public int Total { get; init; }

    /// <summary>Номер страницы, начиная с единицы.</summary>
    public int Page { get; init; }

    /// <summary>Максимальное количество заказов на странице.</summary>
    public int PageSize { get; init; }

    /// <summary>Создаёт модель «Страница заказов с общим количеством результатов».</summary>
    /// <param name="items">Заказы текущей страницы.</param>
    /// <param name="total">Количество заказов, соответствующих условиям поиска.</param>
    /// <param name="page">Номер страницы, начиная с единицы.</param>
    /// <param name="pageSize">Максимальное количество заказов на странице.</param>
    public OrderPage(IReadOnlyList<OrderDto> items, int total, int page, int pageSize)
    {
        Items = items;
        Total = total;
        Page = page;
        PageSize = pageSize;
    }
}
