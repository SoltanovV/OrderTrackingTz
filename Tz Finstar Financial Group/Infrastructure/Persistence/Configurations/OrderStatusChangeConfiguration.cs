using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderTracking.Domain.Models.Entity;

namespace OrderTracking.Infrastructure.Persistence.Configurations;

/// <summary>Настройка хранения истории и уникальности записи для каждой версии заказа.</summary>
internal sealed class OrderStatusChangeConfiguration : IEntityTypeConfiguration<OrderStatusChange>
{
    public void Configure(EntityTypeBuilder<OrderStatusChange> builder)
    {
        builder.ToTable("OrderStatusChange");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => new { x.OrderId, x.Version }).IsUnique();
    }
}
