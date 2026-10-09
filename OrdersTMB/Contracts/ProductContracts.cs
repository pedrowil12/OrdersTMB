using System.ComponentModel.DataAnnotations;

namespace OrdersTMB.Contracts;

public sealed class CreateProductRequest
{
    [Required, MinLength(2), MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal Price { get; set; }
}

public sealed record ProductResponse(
    Guid Id,
    string Name,
    decimal Price,
    bool Active);
