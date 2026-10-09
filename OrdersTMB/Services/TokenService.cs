using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OrdersTMB.Models;

namespace OrdersTMB.Services;

public sealed class TokenService(IConfiguration configuration)
{
    // Gera um token JWT para o usuário fornecido
    public (string Token, DateTime ExpiresAtUtc) Create(User user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("A chave JWT não foi configurada.");

        if (key.Length < 32)
        {
            throw new InvalidOperationException("A chave JWT deve ter pelo menos 32 caracteres.");
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(
            configuration.GetValue("Jwt:ExpirationMinutes", 120));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.ObterNomeLegivel()),
            new Claim(ClaimTypes.Email, user.ObterEmailLegivel()),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
