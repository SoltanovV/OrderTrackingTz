using OrderTracking.Application.Models.Events;

namespace OrderTracking.Application.Services.Interface;

/// <summary>Транзакционная граница для заказа, его истории и исходящего события.</summary>
public interface IOrderUnitOfWork
{
    /// <summary>Атомарно сохраняет изменения заказа и событие для последующей отправки в брокер.</summary>
    Task SaveAsync(OrderChanged change, CancellationToken ct);
}
