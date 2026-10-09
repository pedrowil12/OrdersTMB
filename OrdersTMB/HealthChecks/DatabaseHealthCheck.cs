using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrdersTMB.Data;

namespace OrdersTMB.HealthChecks;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await database.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL disponível.")
                : HealthCheckResult.Unhealthy("PostgreSQL indisponível.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Falha ao acessar o PostgreSQL.", exception);
        }
    }
}
