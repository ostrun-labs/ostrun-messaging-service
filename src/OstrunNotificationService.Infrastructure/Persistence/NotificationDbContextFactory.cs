using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OstrunNotificationService.Infrastructure.Persistence;

// Used by `dotnet ef migrations` at design time, where the Api's DI container
// isn't available. The connection string here only needs to be valid enough for
// Npgsql to generate SQL; it's never actually opened by the CLI tooling for
// `migrations add`.
public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=ostrun_notification;Username=postgres;Password=postgres");

        return new NotificationDbContext(optionsBuilder.Options);
    }
}
