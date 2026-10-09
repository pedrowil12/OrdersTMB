using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace OrdersTMB.HealthChecks;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = configuration["RabbitMq:Uri"]
                ?? throw new InvalidOperationException("A URI do RabbitMQ não foi configurada.");

            var factory = new ConnectionFactory { Uri = new Uri(uri) };
            await using var connection = await factory.CreateConnectionAsync(
                "orders-api-health",
                cancellationToken);

            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ disponível.")
                : HealthCheckResult.Unhealthy("RabbitMQ indisponível.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Falha ao acessar o RabbitMQ.", exception);
        }
    }
}
