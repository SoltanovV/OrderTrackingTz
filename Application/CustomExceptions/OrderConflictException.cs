namespace OrderTracking.Application.CustomExceptions;

/// <summary>Конфликт уникальности, версии или допустимого состояния заказа.</summary>
/// <param name="message">Описание конфликта для пользователя.</param>
public sealed class OrderConflictException(string message) : Exception(message);
