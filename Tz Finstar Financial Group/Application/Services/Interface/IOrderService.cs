using OrderTracking.Application.Models.Request;
using OrderTracking.Application.Models.Response;

namespace OrderTracking.Application.Services.Interface;

/// <summary>Прикладные операции создания, поиска и изменения заказов.</summary>
public interface IOrderService
{
    /// <summary>Возвращает страницу заказов, соответствующих условиям поиска.</summary>
    Task<OrderPage> ListAsync(GetOrdersRequest request, CancellationToken ct);

    /// <summary>Возвращает заказ с историей по его идентификатору.</summary>
    Task<OrderDto> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Создаёт заказ и атомарно сохраняет событие его создания.</summary>
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct);

    /// <summary>Меняет статус после проверки версии и сохраняет событие изменения.</summary>
    Task<OrderDto> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken ct);
}
