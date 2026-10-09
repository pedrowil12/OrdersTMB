namespace OrdersTMB.Shared.Messaging;

public static class MessagingConstants
{
    public const string OrderCreatedEvent = "OrderCreated";
    public const string OrderStatusChangedEvent = "OrderStatusChanged";
    public const string DefaultQueue = "orders.created";
    public const string StatusQueue = "orders.status.changed";
}
