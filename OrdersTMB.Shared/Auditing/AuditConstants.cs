namespace OrdersTMB.Shared.Auditing;

public static class AuditConstants
{
    public const string OrdersTable = "Orders";
    public const string OrderCreatedAction = "Pedido criado pelo usuário";
    public const string OrderStatusChangedAction = "Status alterado pelo Worker";
    public const string OrderHistoryImportedAction = "Histórico importado pela migration";
}
