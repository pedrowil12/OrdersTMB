using System.Text.Json;
using Npgsql;
using OrdersTMB.Shared.Auditing;
using OrdersTMB.Shared.Messaging;
using OrdersTMB.Shared.Orders;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrdersTMB.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IConfiguration configuration) : BackgroundService
{
    private static readonly TimeSpan StatusDuration = TimeSpan.FromSeconds(5);

    //Iniciador de consumo de mensagens do RabbitMQ
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
                logger.LogError(exception, "Falha no consumidor. Nova tentativa em 5 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    // Consome mensagens de pedidos criados do RabbitMQ e processa os pedidos
    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var rabbitUri = configuration["RabbitMq:Uri"]
            ?? throw new InvalidOperationException("A URI do RabbitMQ não foi configurada.");
        var createdQueue = configuration["RabbitMq:Queue"] ?? MessagingConstants.DefaultQueue;
        var statusQueue = configuration["RabbitMq:StatusQueue"] ?? MessagingConstants.StatusQueue;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitUri),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ConsumerDispatchConcurrency = 1
        };


        await using var connection = await factory.CreateConnectionAsync(
            "orders-worker",
            stoppingToken);

        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            stoppingToken);

        await DeclareQueueAsync(channel, createdQueue, stoppingToken);
        await DeclareQueueAsync(channel, statusQueue, stoppingToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        await RecoverIncompleteOrdersAsync(channel, statusQueue, stoppingToken);

        // Configura o consumidor assíncrono para processar mensagens de pedidos criados
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            OrderCreatedMessage message;

            try
            {
                message = JsonSerializer.Deserialize<OrderCreatedMessage>(eventArgs.Body.Span)
                    ?? throw new InvalidDataException("A mensagem está vazia.");
                Validate(message);
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                logger.LogWarning(exception, "Mensagem inválida descartada.");
                await channel.BasicRejectAsync(
                    eventArgs.DeliveryTag,
                    requeue: false,
                    cancellationToken: stoppingToken);
                return;
            }

            try
            {
                await ProcessOrderAsync(message.OrderId, channel, statusQueue, stoppingToken);
                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // O RabbitMQ devolve a mensagem não confirmada quando o canal é fechado.
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha ao processar a mensagem do pedido.");
                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: createdQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Worker aguardando mensagens na fila {Queue}.", createdQueue);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }


    // Processa o pedido, atualizando seu status e publicando eventos de status
    private async Task ProcessOrderAsync(
        Guid orderId,
        IChannel channel,
        string statusQueue,
        CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A conexão com o PostgreSQL não foi configurada.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var initialState = await GetOrderStateAsync(connection, orderId, cancellationToken);
        if (initialState is null)
        {
            throw new InvalidOperationException($"Pedido {orderId} ainda não está disponível.");
        }

        if (initialState.Status == OrderStatuses.Finished)
        {
            await PublishStatusAsync(
                channel,
                statusQueue,
                orderId,
                initialState.CustomerId,
                OrderStatuses.Finished,
                cancellationToken);
            logger.LogInformation("Pedido {OrderId} já estava finalizado.", orderId);
            return;
        }

        if (initialState.Status == OrderStatuses.Pending)
        {
            await PublishStatusAsync(
                channel,
                statusQueue,
                orderId,
                initialState.CustomerId,
                OrderStatuses.Pending,
                cancellationToken);
            logger.LogInformation("Pedido {OrderId} pendente.", orderId);
            await Task.Delay(StatusDuration, cancellationToken);
        }
        else if (initialState.Status != OrderStatuses.Processing)
        {
            logger.LogWarning(
                "Pedido {OrderId} possui o status desconhecido {Status}.",
                orderId,
                initialState.Status);
            return;
        }

        var customerId = await UpdateStatusAsync(
            connection,
            orderId,
            OrderStatuses.Pending,
            OrderStatuses.Processing,
            cancellationToken);

        if (customerId.HasValue)
        {
            await PublishStatusAsync(
                channel,
                statusQueue,
                orderId,
                customerId.Value,
                OrderStatuses.Processing,
                cancellationToken);
        }
        else
        {
            var current = await GetOrderStateAsync(connection, orderId, cancellationToken);

            if (current is null)
            {
                throw new InvalidOperationException($"Pedido {orderId} ainda não está disponível.");
            }

            customerId = current.CustomerId;

            if (current.Status == OrderStatuses.Finished)
            {
                await PublishStatusAsync(
                    channel,
                    statusQueue,
                    orderId,
                    customerId.Value,
                    OrderStatuses.Finished,
                    cancellationToken);
                logger.LogInformation("Pedido {OrderId} já estava finalizado.", orderId);
                return;
            }

            if (current.Status == OrderStatuses.Pending)
            {
                throw new InvalidOperationException(
                    $"Pedido {orderId} ainda aguarda a transição para processamento.");
            }

            if (current.Status != OrderStatuses.Processing)
            {
                logger.LogWarning(
                    "Pedido {OrderId} possui o status desconhecido {Status}.",
                    orderId,
                    current.Status);
                return;
            }

            await PublishStatusAsync(
                channel,
                statusQueue,
                orderId,
                customerId.Value,
                OrderStatuses.Processing,
                cancellationToken);
        }

        logger.LogInformation("Pedido {OrderId} em processamento.", orderId);
        await Task.Delay(StatusDuration, cancellationToken);

        var finishedCustomerId = await UpdateStatusAsync(
            connection,
            orderId,
            OrderStatuses.Processing,
            OrderStatuses.Finished,
            cancellationToken);

        if (finishedCustomerId.HasValue)
        {
            customerId = finishedCustomerId;
        }
        else
        {
            var current = await GetOrderStateAsync(connection, orderId, cancellationToken);
            if (current is null || current.Status != OrderStatuses.Finished)
            {
                throw new InvalidOperationException(
                    $"Pedido {orderId} não concluiu a transição para finalizado.");
            }

            customerId = current.CustomerId;
        }

        await PublishStatusAsync(
            channel,
            statusQueue,
            orderId,
            customerId.Value,
            OrderStatuses.Finished,
            cancellationToken);

        logger.LogInformation("Pedido {OrderId} finalizado.", orderId);
    }


    // Recupera pedidos incompletos do banco de dados e os processa
    private async Task RecoverIncompleteOrdersAsync(
        IChannel channel,
        string statusQueue,
        CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A conexão com o PostgreSQL não foi configurada.");

        var orderIds = new List<Guid>();
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT "Id"
                  FROM "Orders"
                 WHERE "Status" IN (@pending, @processing)
                 ORDER BY "DataCriacao";
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("pending", OrderStatuses.Pending);
            command.Parameters.AddWithValue("processing", OrderStatuses.Processing);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                orderIds.Add(reader.GetGuid(0));
            }
        }

        if (orderIds.Count == 0)
        {
            return;
        }

        logger.LogInformation("Recuperando {Count} pedido(s) incompleto(s).", orderIds.Count);

        foreach (var orderId in orderIds)
        {
            await ProcessOrderAsync(orderId, channel, statusQueue, cancellationToken);
        }
    }

    // Atualiza o status do pedido no banco de dados e registra a alteração no log de auditoria
    private static async Task<Guid?> UpdateStatusAsync(
        NpgsqlConnection connection,
        Guid orderId,
        string expectedStatus,
        string newStatus,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sql = """
            UPDATE "Orders"
               SET "Status" = @newStatus
             WHERE "Id" = @orderId
               AND "Status" = @expectedStatus
            RETURNING "UserId";
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("orderId", orderId);
        command.Parameters.AddWithValue("expectedStatus", expectedStatus);
        command.Parameters.AddWithValue("newStatus", newStatus);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not Guid customerId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        const string auditSql = """
            INSERT INTO "LogsAudit"
                ("Id", "UserId", "EntityId", "Action", "Table", "Dado", "CreateDate")
            VALUES
                (@id, @userId, @entityId, @action, @table, @data, @createdAtUtc);
            """;
        await using var auditCommand = new NpgsqlCommand(auditSql, connection, transaction);
        auditCommand.Parameters.AddWithValue("id", Guid.NewGuid());
        auditCommand.Parameters.AddWithValue("userId", customerId);
        auditCommand.Parameters.AddWithValue("entityId", orderId);
        auditCommand.Parameters.AddWithValue("action", AuditConstants.OrderStatusChangedAction);
        auditCommand.Parameters.AddWithValue("table", AuditConstants.OrdersTable);
        auditCommand.Parameters.AddWithValue(
            "data",
            JsonSerializer.Serialize(new OrderStatusAuditData(
                orderId,
                expectedStatus,
                newStatus)));
        auditCommand.Parameters.AddWithValue("createdAtUtc", DateTime.UtcNow);
        await auditCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return customerId;
    }

    private static async Task<OrderState?> GetOrderStateAsync(
        NpgsqlConnection connection,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT \"UserId\", \"Status\" FROM \"Orders\" WHERE \"Id\" = @orderId;";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", orderId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new OrderState(reader.GetGuid(0), reader.GetString(1))
            : null;
    }

    // Publica o status do pedido no RabbitMQ
    private static async Task PublishStatusAsync(
        IChannel channel,
        string statusQueue,
        Guid orderId,
        Guid customerId,
        string status,
        CancellationToken cancellationToken)
    {
        var message = new OrderStatusChangedMessage(
            orderId,
            customerId,
            orderId,
            MessagingConstants.OrderStatusChangedEvent,
            status,
            DateTime.UtcNow);
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            CorrelationId = orderId.ToString(),
            MessageId = $"{orderId}:{status}",
            Type = MessagingConstants.OrderStatusChangedEvent,
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: statusQueue,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    // Declara a fila no RabbitMQ, garantindo que ela exista antes de consumir ou publicar mensagens
    private static Task DeclareQueueAsync(
        IChannel channel,
        string queue,
        CancellationToken cancellationToken) =>
        channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: cancellationToken);

    private static void Validate(OrderCreatedMessage message)
    {
        if (message.OrderId == Guid.Empty ||
            message.CorrelationId != message.OrderId ||
            message.EventType != MessagingConstants.OrderCreatedEvent)
        {
            throw new InvalidDataException("A mensagem não representa um OrderCreated válido.");
        }
    }

    private sealed record OrderState(Guid CustomerId, string Status);
}
