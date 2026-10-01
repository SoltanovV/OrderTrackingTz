namespace OrderTracking.Infrastructure.Models.Settings;

/// <summary>Параметры подключения и публикации событий в RabbitMQ.</summary>
public sealed class RabbitMqOptions
{
    /// <summary>Имя узла брокера сообщений.</summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>Порт подключения по протоколу AMQP.</summary>
    public int Port { get; set; } = 5672;

    /// <summary>Имя пользователя брокера.</summary>
    public string UserName { get; set; } = "orders";

    /// <summary>Пароль пользователя брокера; для развёртывания задаётся конфигурацией.</summary>
    public string Password { get; set; } = "orders_dev";

    /// <summary>Название обменника для событий заказов.</summary>
    public string Exchange { get; set; } = "orders.status";
}
