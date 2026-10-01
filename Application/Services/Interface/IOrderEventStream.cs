namespace OrderTracking.Application.Services.Interface;

/// <summary>Создание независимых подписок на изменения заказов.</summary>
public interface IOrderEventStream
{
    /// <summary>Подписывает клиента на выбранный заказ либо на все заказы.</summary>
    IOrderEventSubscription Subscribe(Guid? orderId);
}
