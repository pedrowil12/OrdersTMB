using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OrdersTMB.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = ToDisplayStatus(report.Status),
            checks = report.Entries
                .OrderBy(entry => DisplayOrder(entry.Key))
                .Select(entry => new
                {
                    name = entry.Key,
                    status = ToDisplayStatus(entry.Value.Status),
                    description = entry.Value.Description,
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
                })
        };

        return context.Response.WriteAsJsonAsync(response);
    }

    private static string ToDisplayStatus(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "check",
        HealthStatus.Degraded => "warning",
        _ => "unchecked"
    };

    private static int DisplayOrder(string name) => name switch
    {
        "API" => 0,
        "Worker" => 1,
        "PostgreSQL" => 2,
        "RabbitMQ" => 3,
        _ => 4
    };
}
