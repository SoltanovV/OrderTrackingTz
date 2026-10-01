using OrderTracking.Application.Services.Interface;

namespace OrderTracking.Infrastructure.Models.Settings;

/// <summary>Потокобезопасное состояние подключения фонового сервиса к брокеру.</summary>
public sealed class BrokerStatus : IBrokerStatus
{
    private volatile bool connected;

    /// <summary>Признак готовности соединения и подписки на события.</summary>
    public bool Connected
    {
        get => connected;
        set => connected = value;
    }
}
