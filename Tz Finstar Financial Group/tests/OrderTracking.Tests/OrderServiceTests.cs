using Microsoft.Extensions.Logging.Abstractions;
using OrderTracking.Application.CustomExceptions;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Models.Query;
using OrderTracking.Application.Models.Request;
using OrderTracking.Application.Services;
using OrderTracking.Application.Services.Interface;
using OrderTracking.Domain.Models.Entity;
using OrderTracking.Domain.Models.Enums;
using Xunit;

namespace OrderTracking.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateNormalizesInputAndSavesAnEventThroughTheTransactionBoundary()
    {
        var store = new TestOrderStore();
        var service = CreateService(store);

        var result = await service.CreateAsync(new CreateOrderRequest
        {
            OrderNumber = "  test-1  ",
            Description = "  Описание  "
        }, CancellationToken.None);

        Assert.Equal("TEST-1", result.OrderNumber);
        Assert.Equal("Описание", result.Description);
        Assert.Equal(result, Assert.Single(store.SavedEvents).Order);
        Assert.Equal(store.Order!.Id, result.Id);
    }

    [Fact]
    public async Task DuplicateNumberDoesNotAddAnOrderOrSaveAnEvent()
    {
        var store = new TestOrderStore { NumberExists = true };
        var service = CreateService(store);

        await Assert.ThrowsAsync<OrderConflictException>(() => service.CreateAsync(
            new CreateOrderRequest { OrderNumber = "duplicate", Description = "Описание" }, CancellationToken.None));

        Assert.Null(store.Order);
        Assert.Empty(store.SavedEvents);
    }

    [Fact]
    public async Task StaleVersionDoesNotModifyTheAggregateOrSaveAnEvent()
    {
        var store = new TestOrderStore { Order = Order.Create("TEST", "Описание", DateTime.UtcNow) };
        var service = CreateService(store);

        await Assert.ThrowsAsync<OrderConflictException>(() => service.ChangeStatusAsync(store.Order.Id,
            new ChangeStatusRequest { Status = OrderStatus.Shipped, Version = 2 }, CancellationToken.None));

        Assert.Equal(OrderStatus.Created, store.Order.Status);
        Assert.Single(store.Order.History);
        Assert.Empty(store.SavedEvents);
    }

    [Fact]
    public async Task RepeatingTheCurrentStatusDoesNotSaveAnEvent()
    {
        var store = new TestOrderStore { Order = Order.Create("TEST", "Описание", DateTime.UtcNow) };
        var service = CreateService(store);

        var result = await service.ChangeStatusAsync(store.Order.Id,
            new ChangeStatusRequest { Status = OrderStatus.Created, Version = 1 }, CancellationToken.None);

        Assert.Equal(1, result.Version);
        Assert.Empty(store.SavedEvents);
    }

    [Fact]
    public async Task PersistenceFailureIsNotReportedAsSuccessfulCreation()
    {
        var store = new TestOrderStore { FailOnSave = true };
        var service = CreateService(store);

        await Assert.ThrowsAsync<IOException>(() => service.CreateAsync(
            new CreateOrderRequest { OrderNumber = "TEST", Description = "Описание" }, CancellationToken.None));

        Assert.Empty(store.SavedEvents);
    }

    private static OrderService CreateService(TestOrderStore store)
        => new(store, store, TimeProvider.System, NullLogger<OrderService>.Instance);

    private sealed class TestOrderStore : IOrderRepository, IOrderUnitOfWork
    {
        public Order? Order { get; set; }
        public bool NumberExists { get; init; }
        public bool FailOnSave { get; init; }
        public List<OrderChanged> SavedEvents { get; } = [];

        public Task<OrderSearchResult> ListAsync(GetOrdersRequest request, CancellationToken ct)
            => Task.FromResult(new OrderSearchResult(Order is null ? [] : [Order], Order is null ? 0 : 1));

        public Task<Order?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(Order);
        public Task<bool> ExistsAsync(string orderNumber, CancellationToken ct) => Task.FromResult(NumberExists);
        public void Add(Order order) => Order = order;

        public Task SaveAsync(OrderChanged change, CancellationToken ct)
        {
            if (FailOnSave) throw new IOException("Хранилище недоступно.");
            SavedEvents.Add(change);
            return Task.CompletedTask;
        }
    }
}
