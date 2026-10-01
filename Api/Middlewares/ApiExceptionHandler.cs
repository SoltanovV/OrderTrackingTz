using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderTracking.Application.CustomExceptions;

namespace OrderTracking.Api.Middlewares;

/// <summary>Преобразует прикладные и непредвиденные исключения в единый формат HTTP-ошибок.</summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Формирует ответ ProblemDetails и скрывает внутренние детали непредвиденных ошибок.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            OrderNotFoundException => (404, "Заказ не найден."),
            OrderConflictException => (409, exception.Message),
            _ => (500, "Не удалось выполнить запрос. Повторите попытку позже.")
        };
        if (status == 500) logger.LogError(exception, "Unhandled request error {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, ct);
        return true;
    }
}
