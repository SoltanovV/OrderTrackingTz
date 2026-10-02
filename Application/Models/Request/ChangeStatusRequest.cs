using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using OrderTracking.Domain.Models.Enums;

namespace OrderTracking.Application.Models.Request;

/// <summary>Данные для изменения статуса с проверкой версии заказа.</summary>
public sealed class ChangeStatusRequest
{
    /// <summary>Новый статус заказа.</summary>
    [JsonRequired]
    [EnumDataType(typeof(OrderStatus), ErrorMessage = "Неизвестный статус.")]
    public OrderStatus Status { get; init; }

    /// <summary>Версия заказа, полученная клиентом перед изменением.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Укажите актуальную версию заказа.")]
    public int Version { get; init; }
}
