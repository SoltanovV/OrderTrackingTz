using OrderTracking.Domain.Models.Enums;

namespace OrderTracking.Application.Models.Response;

/// <summary>Данные заказа и его история для передачи клиенту.</summary>
public sealed record OrderDto
{
    /// <summary>Уникальный идентификатор заказа.</summary>
    public Guid Id { get; init; }

    /// <summary>Уникальный номер заказа.</summary>
    public string OrderNumber { get; init; }

    /// <summary>Описание содержимого заказа.</summary>
    public string Description { get; init; }

    /// <summary>Текущий статус заказа.</summary>
    public OrderStatus Status { get; init; }

    /// <summary>Дата и время создания в UTC.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Дата и время последнего изменения в UTC.</summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>Версия заказа для проверки конкурентных изменений.</summary>
    public int Version { get; init; }

    /// <summary>История статусов, упорядоченная по версии заказа.</summary>
    public IReadOnlyList<HistoryItem> History { get; init; }

    /// <summary>Создаёт модель «Данные заказа и его история для передачи клиенту».</summary>
    /// <param name="id">Уникальный идентификатор заказа.</param>
    /// <param name="orderNumber">Уникальный номер заказа.</param>
    /// <param name="description">Описание содержимого заказа.</param>
    /// <param name="status">Текущий статус заказа.</param>
    /// <param name="createdAt">Дата и время создания в UTC.</param>
    /// <param name="updatedAt">Дата и время последнего изменения в UTC.</param>
    /// <param name="version">Версия заказа для проверки конкурентных изменений.</param>
    /// <param name="history">История статусов, упорядоченная по версии заказа.</param>
    public OrderDto(Guid id, string orderNumber, string description, OrderStatus status, DateTime createdAt, DateTime updatedAt, int version, IReadOnlyList<HistoryItem> history)
    {
        Id = id;
        OrderNumber = orderNumber;
        Description = description;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Version = version;
        History = history;
    }
}
