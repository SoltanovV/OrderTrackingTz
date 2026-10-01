using Microsoft.EntityFrameworkCore;
using OrderTracking.Domain.Models.Entity;
using OrderTracking.Infrastructure.Models.Entity;

namespace OrderTracking.Infrastructure.Persistence;

/// <summary>Контекст хранения заказов, истории статусов и исходящих событий.</summary>
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    /// <summary>Заказы, зарегистрированные в системе.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>События, ожидающие подтверждённой отправки в брокер.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Подключает отдельные конфигурации таблиц и связей сущностей.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
}
