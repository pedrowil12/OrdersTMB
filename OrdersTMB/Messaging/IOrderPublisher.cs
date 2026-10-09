using OrdersTMB.Shared.Messaging;

namespace OrdersTMB.Messaging;

public interface IOrderPublisher
{
    Task PublishAsync(OrderCreatedMessage message, CancellationToken cancellationToken);
}
