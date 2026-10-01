namespace OrderTracking.Application.Services.Interface;

/// <summary>Доступное для чтения состояние подключения к брокеру сообщений.</summary>
public interface IBrokerStatus
{
    /// <summary>Признак установленного подключения с готовым потребителем сообщений.</summary>
    bool Connected { get; }
}
