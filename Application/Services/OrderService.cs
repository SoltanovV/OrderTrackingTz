using Microsoft.Extensions.Logging;
using OrderTracking.Application.Constants;
using OrderTracking.Application.CustomExceptions;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Models.Request;
using OrderTracking.Application.Models.Response;
using OrderTracking.Application.Services.Interface;
using OrderTracking.Application.Utilities;
using OrderTracking.Domain.Models.Entity;

namespace OrderTracking.Application.Services;

/// <summary>Реализует сценарии работы с заказами через абстракции хранилища и транзакции.</summary>
public sealed class OrderService(IOrderRepository repository, IOrderUnitOfWork unitOfWork,
    TimeProvider clock, ILogger<OrderService> logger) : IOrderService
{
    /// <summary>Возвращает страницу заказов в представлении для клиента.</summary>
    public async Task<OrderPage> ListAsync(GetOrdersRequest request, CancellationToken ct)
    {
        var result = await repository.ListAsync(request, ct);
        return new OrderPage(result.Items.Select(x => x.ToDto()).ToArray(), result.Total, request.Page, request.PageSize);
    }

    /// <summary>Загружает заказ и преобразует его в модель ответа.</summary>
    public async Task<OrderDto> GetAsync(Guid id, CancellationToken ct) => (await FindAsync(id, ct)).ToDto();

    /// <summary>Проверяет уникальность номера и создаёт заказ вместе с исходящим событием.</summary>
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var number = request.OrderNumber.Trim().ToUpperInvariant();
        if (await repository.ExistsAsync(number, ct))
            throw new OrderConflictException(OrderErrorMessages.DuplicateNumber);

        var order = Order.Create(number, request.Description.Trim(), clock.GetUtcNow().UtcDateTime);
        repository.Add(order);
        var result = order.ToDto();
        await unitOfWork.SaveAsync(new OrderChanged(Guid.NewGuid(), result), ct);
        logger.LogInformation("Создан заказ {OrderId} с номером {OrderNumber}", order.Id, order.OrderNumber);
        return result;
    }

    /// <summary>Проверяет версию заказа, изменяет статус и сохраняет событие в общей транзакции.</summary>
    public async Task<OrderDto> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken ct)
    {
        var order = await FindAsync(id, ct);
        if (order.Version != request.Version)
            throw new OrderConflictException(OrderErrorMessages.VersionConflict);

        try
        {
            if (!order.ChangeStatus(request.Status, clock.GetUtcNow().UtcDateTime)) return order.ToDto();
        }
        catch (InvalidOperationException ex)
        {
            throw new OrderConflictException(ex.Message);
        }

        var result = order.ToDto();
        await unitOfWork.SaveAsync(new OrderChanged(Guid.NewGuid(), result), ct);
        logger.LogInformation("Статус заказа {OrderId} изменён на {Status}, версия {Version}", id, order.Status, order.Version);
        return result;
    }

    private async Task<Order> FindAsync(Guid id, CancellationToken ct) =>
        await repository.FindAsync(id, ct) ?? throw new OrderNotFoundException();
}
