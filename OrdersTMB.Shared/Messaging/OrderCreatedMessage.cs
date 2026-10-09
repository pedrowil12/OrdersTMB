namespace OrdersTMB.Shared.Messaging;

public sealed record OrderCreatedMessage(
    Guid OrderId,
    Guid CorrelationId,
    string EventType,
    DateTime OccurredAtUtc);
