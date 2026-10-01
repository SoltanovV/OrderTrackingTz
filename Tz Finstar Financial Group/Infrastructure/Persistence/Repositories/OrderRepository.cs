using Microsoft.EntityFrameworkCore;
using OrderTracking.Application.Models.Query;
using OrderTracking.Application.Models.Request;
using OrderTracking.Application.Services.Interface;
using OrderTracking.Domain.Models.Entity;

namespace OrderTracking.Infrastructure.Persistence.Repositories;

/// <summary>Репозиторий заказов, реализованный с помощью EF Core.</summary>
public sealed class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    /// <summary>Выполняет фильтрацию и постраничное чтение заказов без отслеживания изменений.</summary>
    public async Task<OrderSearchResult> ListAsync(GetOrdersRequest request, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToUpperInvariant();
            query = query.Where(x => x.OrderNumber.ToUpper().Contains(term) || x.Description.ToUpper().Contains(term));
        }
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (request.Ids is { Length: > 0 }) query = query.Where(x => request.Ids.Contains(x.Id));

        var total = await query.CountAsync(ct);
        var orders = await query.Include(x => x.History)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return new OrderSearchResult(orders, total);
    }

    /// <summary>Загружает заказ и его историю с отслеживанием изменений.</summary>
    public Task<Order?> FindAsync(Guid id, CancellationToken ct)
        => db.Orders.Include(x => x.History).SingleOrDefaultAsync(x => x.Id == id, ct);

    /// <summary>Проверяет, занят ли нормализованный номер заказа.</summary>
    public Task<bool> ExistsAsync(string orderNumber, CancellationToken ct)
        => db.Orders.AnyAsync(x => x.OrderNumber == orderNumber, ct);

    /// <summary>Регистрирует новый заказ в контексте текущей операции.</summary>
    public void Add(Order order) => db.Orders.Add(order);
}
