using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using OrdersTMB.Hubs;
using OrdersTMB.Shared.Messaging;
using OrdersTMB.Shared.Orders;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrdersTMB.Messaging;

public sealed class OrderStatusConsumer(
    ILogger<OrderStatusConsumer> logger,
    IConfiguration configuration,
    IHubContext<OrdersHub> hubContext) : BackgroundService
{
    //É chamado quando o serviço é iniciado, e ele inicia o consumo de mensagens do RabbitMQ
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha no consumidor de status. Nova tentativa em 5 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    // Consome mensagens de status do RabbitMQ e envia para os clientes conectados via SignalR
    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var rabbitUri = configuration["RabbitMq:Uri"]
            ?? throw new InvalidOperationException("A URI do RabbitMQ não foi configurada.");
        var statusQueue = configuration["RabbitMq:StatusQueue"] ?? MessagingConstants.StatusQueue;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitUri),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ConsumerDispatchConcurrency = 1
        };

        await using var connection = await factory.CreateConnectionAsync(
            "orders-api-status-consumer",
            stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: statusQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: stoppingToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 20,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            OrderStatusChangedMessage message;

            try
            {
                message = JsonSerializer.Deserialize<OrderStatusChangedMessage>(eventArgs.Body.Span)
                    ?? throw new InvalidDataException("A mensagem está vazia.");
                Validate(message);
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                logger.LogWarning(exception, "Evento de status inválido descartado.");
                await channel.BasicRejectAsync(
                    eventArgs.DeliveryTag,
                    requeue: false,
                    cancellationToken: stoppingToken);
                return;
            }

            try
            {
                await Task.WhenAll(
                    hubContext.Clients.Group(OrdersHub.AdminGroup).SendAsync(
                        MessagingConstants.OrderStatusChangedEvent,
                        message,
                        stoppingToken),
                    hubContext.Clients.Group(OrdersHub.CustomerGroup(message.CustomerId)).SendAsync(
                        MessagingConstants.OrderStatusChangedEvent,
                        message,
                        stoppingToken));

                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // O RabbitMQ devolve o evento não confirmado quando o canal é fechado.
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha ao entregar o status do pedido {OrderId}.", message.OrderId);
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: statusQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("API aguardando eventos de status na fila {Queue}.", statusQueue);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    // Valida se a mensagem recebida é um evento de status de pedido válido
    private static void Validate(OrderStatusChangedMessage message)
    {
        var knownStatus = message.Status is
            OrderStatuses.Pending or
            OrderStatuses.Processing or
            OrderStatuses.Finished;

        if (message.OrderId == Guid.Empty ||
            message.CustomerId == Guid.Empty ||
            message.CorrelationId != message.OrderId ||
            message.EventType != MessagingConstants.OrderStatusChangedEvent ||
            !knownStatus)
        {
            throw new InvalidDataException("A mensagem não representa um OrderStatusChanged válido.");
        }
    }
}
