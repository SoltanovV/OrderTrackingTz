using Microsoft.EntityFrameworkCore;
using OrderTracking.Infrastructure.Persistence;

namespace OrderTracking.Api.Extensions;

/// <summary>Подготовка базы данных при запуске локального экземпляра приложения.</summary>
public static class WebApplicationExtensions
{
    /// <summary>Применяет миграции с ограниченным числом повторов при временной недоступности БД.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing")) return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
                break;
            }
            catch (Exception ex) when (attempt < 10 && !app.Lifetime.ApplicationStopping.IsCancellationRequested)
            {
                app.Logger.LogWarning(ex, "Не удалась попытка миграции БД {Attempt}; повторное подключение", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), app.Lifetime.ApplicationStopping);
            }
        }
    }
}
