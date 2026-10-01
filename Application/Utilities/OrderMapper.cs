using OrderTracking.Application.Models.Response;
using OrderTracking.Domain.Models.Entity;

namespace OrderTracking.Application.Utilities;

/// <summary>Преобразует сущности заказа в модели ответа, не изменяя исходные данные.</summary>
public static class OrderMapper
{
    /// <summary>Создаёт снимок заказа с историей, упорядоченной по версии.</summary>
    /// <param name="order">Исходная сущность заказа.</param>
    /// <returns>Модель заказа для передачи клиенту или публикации события.</returns>
    public static OrderDto ToDto(this Order order) => new(
        order.Id, order.OrderNumber, order.Description, order.Status,
        order.CreatedAt, order.UpdatedAt, order.Version,
        order.History.OrderBy(x => x.Version)
            .Select(x => new HistoryItem(x.Status, x.ChangedAt, x.Version)).ToArray());
}
