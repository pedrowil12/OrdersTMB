using Microsoft.EntityFrameworkCore;
using OrdersTMB.Data;
using OrdersTMB.Models;

namespace OrdersTMB.Services;

public static class AdminSeeder
{

    public static async Task SeedAsync(
        AppDbContext context,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // Verifica se já existe um usuário administrador no banco de dados
        if (await context.Users.AnyAsync(user => user.Role == "admin", cancellationToken))
        {
            return;
        }

        // Obtém as informações do administrador a partir da configuração
        var name = Required(configuration, "Admin:Name");
        var phone = Required(configuration, "Admin:Phone");
        var email = Required(configuration, "Admin:Email").Trim().ToLowerInvariant();
        var password = Required(configuration, "Admin:Password");

        var admin = new User { Role = "admin" };
        admin.DefinirDadosProtegidos(name, phone, email, password);

        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"A configuração obrigatória '{key}' não foi informada.");
}
