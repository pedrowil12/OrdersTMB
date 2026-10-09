using System.ComponentModel.DataAnnotations;

namespace OrdersTMB.Contracts;

public sealed class CreateCustomerRequest
{
    [Required, MinLength(2), MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Phone,
    string Email,
    bool Active,
    DateTime CreatedAtUtc);
