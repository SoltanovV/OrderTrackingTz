using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderTracking.Application.Services.Interface;
using OrderTracking.Infrastructure.Models.Settings;
using OrderTracking.Infrastructure.Persistence;
using OrderTracking.Infrastructure.Persistence.Repositories;
using OrderTracking.Infrastructure.Services;
using OrderTracking.Infrastructure.Services.Background;

namespace OrderTracking.Infrastructure.Extensions;

/// <summary>Регистрация реализаций доступа к данным, брокеру и потокам уведомлений.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Подключает PostgreSQL, репозиторий, транзакции, RabbitMQ и потоки событий.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("Orders")
            ?? throw new InvalidOperationException("Не задана строка подключения к базе заказов.")));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderUnitOfWork, OrderUnitOfWork>();
        services.AddScoped<IReadinessService, ReadinessService>();
        services.AddSingleton<EventHub>();
        services.AddSingleton<IOrderEventStream>(provider => provider.GetRequiredService<EventHub>());
        services.AddSingleton<IOrderEventPublisher>(provider => provider.GetRequiredService<EventHub>());
        services.AddSingleton<BrokerStatus>();
        services.AddSingleton<IBrokerStatus>(provider => provider.GetRequiredService<BrokerStatus>());
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddHostedService<RabbitMqWorker>();
        return services;
    }
}
