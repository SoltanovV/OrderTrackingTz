namespace OrderTracking.Infrastructure.Models.Entity;

/// <summary>Исходящее событие, сохраняемое в одной транзакции с заказом до подтверждения брокера.</summary>
public sealed class OutboxMessage
{
    /// <summary>Уникальный идентификатор события.</summary>
    public Guid Id { get; set; }

    /// <summary>Содержимое события в формате JSON.</summary>
    public string Payload { get; set; } = "";

    /// <summary>Дата и время возникновения события в UTC.</summary>
    public DateTime CreatedAt { get; set; }
}
