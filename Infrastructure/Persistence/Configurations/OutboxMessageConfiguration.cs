using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderTracking.Infrastructure.Models.Entity;

namespace OrderTracking.Infrastructure.Persistence.Configurations;

/// <summary>Настройка таблицы событий, ожидающих отправки в брокер.</summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Payload).IsRequired();
        builder.HasIndex(x => x.CreatedAt);
    }
}
