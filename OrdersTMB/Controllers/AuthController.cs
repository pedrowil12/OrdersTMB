using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersTMB.Contracts;
using OrdersTMB.Data;
using OrdersTMB.Services;

namespace OrdersTMB.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(AppDbContext context, TokenService tokenService) : ControllerBase
{
    // Endpoint para login de usuário
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var encryptedEmail = EncryptionService.Criptografar(normalizedEmail);

        // Busca o usuário no banco de dados com base no e-mail criptografado
        var user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Email == encryptedEmail && item.Active,
                cancellationToken);

        if (user is null || !PasswordService.Verificar(request.Password, user.SenhaHash))
        {
            return Unauthorized(new { message = "E-mail ou senha inválidos." });
        }

        // Gera o token JWT para o usuário autenticado
        var (token, expiresAtUtc) = tokenService.Create(user);
        var response = new LoginResponse(
            token,
            expiresAtUtc,
            new AuthenticatedUserResponse(
                user.Id,
                user.ObterNomeLegivel(),
                user.ObterEmailLegivel(),
                user.Role));

        return Ok(response);
    }
}
