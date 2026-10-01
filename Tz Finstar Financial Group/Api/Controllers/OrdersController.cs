using Microsoft.AspNetCore.Mvc;
using OrderTracking.Application.Models.Request;
using OrderTracking.Application.Models.Response;
using OrderTracking.Application.Services.Interface;

namespace OrderTracking.Api.Controllers;

/// <summary>HTTP операции создания поиска и изменения статуса заказов.</summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>Возвращает страницу заказов по указанным условиям поиска.</summary>
    [HttpGet]
    public async Task<ActionResult<OrderPage>> List([FromQuery] GetOrdersRequest request, CancellationToken ct)
        => Ok(await orders.ListAsync(request, ct));

    /// <summary>Возвращает карточку заказа с полной историей статусов.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct)
        => Ok(await orders.GetAsync(id, ct));

    /// <summary>Создаёт заказ и возвращает адрес его карточки.</summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await orders.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    /// <summary>Изменяет статус заказа с проверкой его версии.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct)
        => Ok(await orders.ChangeStatusAsync(id, request, ct));
}
