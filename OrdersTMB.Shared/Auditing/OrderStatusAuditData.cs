namespace OrdersTMB.Shared.Auditing;

public sealed record OrderStatusAuditData(
    Guid OrderId,
    string? PreviousStatus,
    string NewStatus);
