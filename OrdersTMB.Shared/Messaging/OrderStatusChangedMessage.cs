namespace OrdersTMB.Shared.Messaging;

public sealed record OrderStatusChangedMessage(
    Guid OrderId,
    Guid CustomerId,
    Guid CorrelationId,
    string EventType,
    string Status,
    DateTime OccurredAtUtc);
