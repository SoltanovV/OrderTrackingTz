using Microsoft.Extensions.DependencyInjection;
using OrderTracking.Application.Services;
using OrderTracking.Application.Services.Interface;

namespace OrderTracking.Application.Extensions;

/// <summary>Регистрация прикладных сценариев, не зависящих от конкретных внешних сервисов.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Регистрирует сервис заказов и системный источник времени.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
