using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrdersTMB.Worker;
using OrdersTMB.Worker.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHealthChecks()
    .AddCheck("Worker", () => HealthCheckResult.Healthy("Worker em execução."))
    .AddCheck<DatabaseHealthCheck>("PostgreSQL")
    .AddCheck<RabbitMqHealthCheck>("RabbitMQ");

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

await app.RunAsync();
