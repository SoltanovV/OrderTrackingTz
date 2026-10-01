namespace OrderTracking.Application.Models.Response;

/// <summary>Ответ проверки работоспособности процесса приложения.</summary>
public sealed class LivenessResponse
{
    /// <summary>Текстовое состояние запущенного процесса.</summary>
    public string Status { get; init; } = "alive";
}
