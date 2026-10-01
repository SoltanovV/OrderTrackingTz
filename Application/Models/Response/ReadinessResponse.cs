namespace OrderTracking.Application.Models.Response;

/// <summary>Состояние внешних зависимостей, необходимых для работы приложения.</summary>
public sealed class ReadinessResponse
{
    /// <summary>Признак доступности базы данных заказов.</summary>
    public bool Database { get; init; }

    /// <summary>Признак подключения к брокеру сообщений.</summary>
    public bool Broker { get; init; }
}
