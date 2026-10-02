using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Services.Interface;
using OrderTracking.Infrastructure.Models.Settings;
using OrderTracking.Infrastructure.Persistence;
using OrderTracking.Infrastructure.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderTracking.Infrastructure.Services.Background;

/// <summary>Отправляет сохранённые события в RabbitMQ и передаёт полученные сообщения подписчикам с повторным подключением при сбоях.</summary>
public sealed class RabbitMqWorker(IServiceScopeFactory scopes, IOptions<RabbitMqOptions> options,
    IOrderEventPublisher hub, BrokerStatus status, ILogger<RabbitMqWorker> logger) : BackgroundService
{
    /// <summary>Поддерживает соединение с брокером и выполняет цикл доставки исходящих событий.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = settings.HostName,
                    Port = settings.Port,
                    UserName = settings.UserName,
                    Password = settings.Password,
                    AutomaticRecoveryEnabled = false,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                    RequestedHeartbeat = TimeSpan.FromSeconds(5)
                };
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                connection.ConnectionShutdownAsync += OnBrokerShutdownAsync;
                await using var consumerChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                consumerChannel.ChannelShutdownAsync += OnBrokerShutdownAsync;
                await consumerChannel.ExchangeDeclareAsync(settings.Exchange, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
                var queue = await consumerChannel.QueueDeclareAsync(cancellationToken: stoppingToken);
                await consumerChannel.QueueBindAsync(queue.QueueName, settings.Exchange, "", cancellationToken: stoppingToken);
                await consumerChannel.BasicQosAsync(0, 32, false, stoppingToken);
                var consumer = new AsyncEventingBasicConsumer(consumerChannel);
                consumer.ReceivedAsync += async (_, delivery) =>
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize<OrderChanged>(delivery.Body.Span, EventJson.Options)
                            ?? throw new JsonException("Empty order event");
                        hub.Publish(message);
                        await consumerChannel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (JsonException ex)
                    {
                        logger.LogError(ex, "Discarding malformed broker event");
                        await consumerChannel.BasicNackAsync(delivery.DeliveryTag, false, false, stoppingToken);
                    }
                };
                await consumerChannel.BasicConsumeAsync(queue.QueueName, false, consumer, stoppingToken);
                await using var publisher = await connection.CreateChannelAsync(
                    new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), stoppingToken);
                publisher.ChannelShutdownAsync += OnBrokerShutdownAsync;
                status.Connected = true;
                if (!connection.IsOpen || !consumerChannel.IsOpen || !publisher.IsOpen)
                    status.Connected = false;
                logger.LogInformation("RabbitMQ connected; consuming queue {Queue}", queue.QueueName);
                while (connection.IsOpen && consumerChannel.IsOpen && publisher.IsOpen && !stoppingToken.IsCancellationRequested)
                {
                    await PublishPendingAsync(publisher, settings.Exchange, stoppingToken);
                    await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            { logger.LogWarning(ex, "Broker relay unavailable; pending events remain in the outbox. Retrying in 5 seconds"); }
            finally { status.Connected = false; }
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private Task OnBrokerShutdownAsync(object sender, ShutdownEventArgs args)
    {
        status.Connected = false;
        return Task.CompletedTask;
    }

    private async Task PublishPendingAsync(IChannel channel, string exchange, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        // Повторная доставка допустима: клиент перечитывает актуальное состояние заказа.
        var batch = await db.OutboxMessages.OrderBy(x => x.CreatedAt).Take(50).ToListAsync(ct);
        foreach (var message in batch)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await channel.BasicPublishAsync(exchange, "", mandatory: true,
                basicProperties: new BasicProperties { Persistent = true, ContentType = "application/json", MessageId = message.Id.ToString() },
                body: Encoding.UTF8.GetBytes(message.Payload), cancellationToken: timeout.Token);
            db.OutboxMessages.Remove(message);
            await db.SaveChangesAsync(ct);
        }
    }
}
