using System.Text.Json;
using RabbitMQ.Client;

namespace OrdersTMB.Messaging;

public sealed class RabbitMqPublisher : IOrderPublisher, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly string _queue;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    // Construtor que inicializa a fábrica de conexões e a fila do RabbitMQ
    public RabbitMqPublisher(IConfiguration configuration)
    {
        var uri = configuration["RabbitMq:Uri"]
            ?? throw new InvalidOperationException("A URI do RabbitMQ não foi configurada.");

        _queue = configuration["RabbitMq:Queue"]
            ?? Shared.Messaging.MessagingConstants.DefaultQueue;

        _factory = new ConnectionFactory
        {
            Uri = new Uri(uri),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
    }

    // Publica uma mensagem de pedido criado no RabbitMQ
    public async Task PublishAsync(
        Shared.Messaging.OrderCreatedMessage message,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                CorrelationId = message.CorrelationId.ToString(),
                MessageId = message.OrderId.ToString(),
                Type = message.EventType,
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queue,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    // Obtém o canal do RabbitMQ, criando uma nova conexão e canal se necessário
    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connection = await _factory.CreateConnectionAsync("orders-api", cancellationToken);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: cancellationToken);

        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
