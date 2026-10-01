using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderTracking.Domain.Models.Entity;

namespace OrderTracking.Infrastructure.Persistence.Configurations;

/// <summary>Настройка хранения заказа, уникальности номера и контроля версии.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OrderNumber).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.CreatedAt);
        builder.HasMany(x => x.History).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.History).HasField("history").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
