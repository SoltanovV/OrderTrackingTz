using System.ComponentModel.DataAnnotations;

namespace OrderTracking.Application.Models.Request;

/// <summary>Данные для создания нового заказа.</summary>
public sealed class CreateOrderRequest
{
    /// <summary>Уникальный номер заказа длиной не более 64 символов.</summary>
    [Required(ErrorMessage = "Укажите номер заказа.")]
    [StringLength(64, ErrorMessage = "Номер заказа должен содержать не более 64 символов.")]
    public string OrderNumber { get; init; } = "";

    /// <summary>Обязательное описание заказа длиной не более 2000 символов.</summary>
    [Required(ErrorMessage = "Укажите описание.")]
    [StringLength(2000, ErrorMessage = "Описание должно содержать не более 2000 символов.")]
    public string Description { get; init; } = "";
}
