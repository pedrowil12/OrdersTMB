using System.ComponentModel.DataAnnotations;

namespace OrdersTMB.Contracts;

public sealed class CreateOrderRequest
{
    [Required, MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public sealed class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public sealed record OrderCustomerResponse(Guid Id, string Name);

public sealed record OrderItemResponse(
    Guid ProductId,
    string Product,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed record OrderHistoryResponse(
    string Status,
    string? PreviousStatus,
    DateTime ChangedAtUtc);

public sealed record OrderResponse(
    Guid Id,
    OrderCustomerResponse Customer,
    IReadOnlyCollection<OrderItemResponse> Products,
    decimal Value,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<OrderHistoryResponse> History);
