namespace OrderTracking.Application.CustomExceptions;

/// <summary>Исключение при отсутствии заказа с указанным идентификатором.</summary>
public sealed class OrderNotFoundException : Exception;
