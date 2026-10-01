using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderTracking.Infrastructure.Utilities;

/// <summary>Единый формат сериализации событий при хранении в outbox и передаче через RabbitMQ.</summary>
public static class EventJson
{
    /// <summary>Параметры JSON с именами свойств в camelCase и строковыми значениями перечислений.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
