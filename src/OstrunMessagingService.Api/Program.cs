using Microsoft.EntityFrameworkCore;
using OstrunMessagingService.Api;
using OstrunMessagingService.Infrastructure;
using OstrunMessagingService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("Messaging");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services
        .AddHealthChecks()
        .AddNpgSql(connectionString, name: "postgres");
}

var app = builder.Build();

// MVP starter: apply migrations on startup instead of a separate migration
// job/step, since each generated instance is a single dev-owned deployment.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<MessagingDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
