using Microsoft.EntityFrameworkCore;
using OstrunNotificationService.Api;
using OstrunNotificationService.Infrastructure;
using OstrunNotificationService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("Notification");
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
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");

app.Run();
