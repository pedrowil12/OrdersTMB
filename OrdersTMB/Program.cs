using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenAI;
using OpenAI.Chat;
using OrdersTMB.Data;
using OrdersTMB.HealthChecks;
using OrdersTMB.Hubs;
using OrdersTMB.Messaging;
using OrdersTMB.Services;
using System.ClientModel;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var encryptionKey = builder.Configuration["Security:EncryptionKey"];
EncryptionService.Inicializar(encryptionKey);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A conexão com o PostgreSQL não foi configurada.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("A chave JWT não foi configurada.");

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("A chave JWT deve ter pelo menos 32 caracteres.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<IOrderPublisher>(provider =>
    provider.GetRequiredService<RabbitMqPublisher>());
builder.Services.AddHostedService<OrderStatusConsumer>();

// Configura a autenticação JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/orders"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

// Configura o Swagger para documentação da API e suporte a autenticação JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrdersTMB API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

// Configura o CORS para permitir solicitações do frontend
var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(frontendUrl)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});


//Configura LLM
builder.Services.AddTransient<ChatClient>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();

    var apiKey = configuration["Groq:ApiKey"]
        ?? throw new InvalidOperationException(
            "A chave da Groq não foi configurada.");

    var options = new OpenAIClientOptions
    {
            Endpoint = new Uri("https://api.groq.com/openai/v1")
    };

    var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);

    return client.GetChatClient("openai/gpt-oss-20b");
});

builder.Services.AddHealthChecks()
    .AddCheck("API", () => HealthCheckResult.Healthy("API disponível."))
    .AddCheck<WorkerHealthCheck>("Worker")
    .AddCheck<DatabaseHealthCheck>("PostgreSQL")
    .AddCheck<RabbitMqHealthCheck>("RabbitMQ");

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrdersHub>("/hubs/orders");
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await AdminSeeder.SeedAsync(context, builder.Configuration);
}

await app.RunAsync();
