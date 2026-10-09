using System.ComponentModel.DataAnnotations;

namespace OrdersTMB.Contracts;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed record AuthenticatedUserResponse(Guid Id, string Name, string Email, string Role);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    AuthenticatedUserResponse User);
