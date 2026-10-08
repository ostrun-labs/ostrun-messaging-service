using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OstrunMessagingService.Infrastructure.Persistence;

// Used by `dotnet ef migrations` at design time, where the Api's DI container
// isn't available. The connection string here only needs to be valid enough for
// Npgsql to generate SQL; it's never actually opened by the CLI tooling for
// `migrations add`.
public sealed class MessagingDbContextFactory : IDesignTimeDbContextFactory<MessagingDbContext>
{
    public MessagingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MessagingDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=ostrun_messaging;Username=postgres;Password=postgres");

        return new MessagingDbContext(optionsBuilder.Options);
    }
}
