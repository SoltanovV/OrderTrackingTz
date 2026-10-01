using OrderTracking.Application.Models.Query;
using OrderTracking.Application.Models.Request;
using OrderTracking.Domain.Models.Entity;

namespace OrderTracking.Application.Services.Interface;

/// <summary>Доступ к заказам без зависимости прикладного слоя от конкретной базы данных.</summary>
public interface IOrderRepository
{
    /// <summary>Читает страницу заказов и общее количество совпадений.</summary>
    Task<OrderSearchResult> ListAsync(GetOrdersRequest request, CancellationToken ct);

    /// <summary>Загружает заказ с историей для чтения или изменения.</summary>
    Task<Order?> FindAsync(Guid id, CancellationToken ct);

    /// <summary>Проверяет наличие заказа с нормализованным номером.</summary>
    Task<bool> ExistsAsync(string orderNumber, CancellationToken ct);

    /// <summary>Добавляет заказ в текущую единицу работы без отдельного сохранения.</summary>
    void Add(Order order);
}
