using System.ComponentModel.DataAnnotations;
using OrderTracking.Domain.Models.Enums;

namespace OrderTracking.Application.Models.Request;

/// <summary>Условия поиска, фильтрации и постраничного получения заказов.</summary>
public sealed class GetOrdersRequest
{
    /// <summary>Подстрока для поиска по номеру или описанию заказа.</summary>
    [StringLength(200, ErrorMessage = "Поисковый запрос должен содержать не более 200 символов.")]
    public string? Search { get; init; }

    /// <summary>Статус для фильтрации; отсутствие значения означает все статусы.</summary>
    [EnumDataType(typeof(OrderStatus), ErrorMessage = "Неизвестный статус.")]
    public OrderStatus? Status { get; init; }

    /// <summary>Идентификаторы отслеживаемых заказов; пустой список не ограничивает поиск.</summary>
    [MaxLength(100, ErrorMessage = "Можно запросить не более 100 идентификаторов.")]
    public Guid[]? Ids { get; init; }

    /// <summary>Номер страницы, начиная с единицы.</summary>
    [Range(1, 100000, ErrorMessage = "Номер страницы должен быть от 1 до 100000.")]
    public int Page { get; init; } = 1;

    /// <summary>Количество заказов на странице от 1 до 100.</summary>
    [Range(1, 100, ErrorMessage = "Размер страницы должен быть от 1 до 100.")]
    public int PageSize { get; init; } = 20;
}
