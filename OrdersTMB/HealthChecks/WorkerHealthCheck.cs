using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OrdersTMB.HealthChecks;

public sealed class WorkerHealthCheck(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workerUrl = configuration["HealthChecks:WorkerUrl"]
                ?? throw new InvalidOperationException("A URL de health check do Worker não foi configurada.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));

            var client = httpClientFactory.CreateClient();
            using var response = await client.GetAsync(workerUrl, timeout.Token);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Worker disponível e dependências verificadas.")
                : HealthCheckResult.Unhealthy($"Worker respondeu com HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Worker indisponível.", exception);
        }
    }
}
