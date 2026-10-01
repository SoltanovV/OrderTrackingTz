using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderTracking.Application.Constants;
using OrderTracking.Application.CustomExceptions;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Services.Interface;
using OrderTracking.Infrastructure.Models.Entity;
using OrderTracking.Infrastructure.Utilities;

namespace OrderTracking.Infrastructure.Persistence;

/// <summary>Атомарное сохранение заказа, истории и события с переводом ошибок БД в прикладные исключения.</summary>
public sealed class OrderUnitOfWork(OrdersDbContext db) : IOrderUnitOfWork
{
    /// <summary>Сохраняет все изменения контекста и запись outbox одной транзакцией EF Core.</summary>
    public async Task SaveAsync(OrderChanged change, CancellationToken ct)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = change.EventId,
            CreatedAt = change.Order.UpdatedAt,
            Payload = JsonSerializer.Serialize(change, EventJson.Options)
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new OrderConflictException(OrderErrorMessages.VersionConflict);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Orders_OrderNumber" })
        {
            throw new OrderConflictException(OrderErrorMessages.DuplicateNumber);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_OrderStatusChange_OrderId_Version" })
        {
            throw new OrderConflictException(OrderErrorMessages.VersionConflict);
        }
    }
}
