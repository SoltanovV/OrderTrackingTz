using System.Threading.Channels;
using OrderTracking.Application.Models.Events;

namespace OrderTracking.Application.Services.Interface;

/// <summary>Подписка клиента на события заказов с освобождением ресурсов при отключении.</summary>
public interface IOrderEventSubscription : IDisposable
{
    /// <summary>Поток событий, доступный подписчику только для чтения.</summary>
    ChannelReader<OrderChanged> Reader { get; }
}
