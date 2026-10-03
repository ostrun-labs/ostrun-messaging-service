namespace OstrunNotificationService.Infrastructure.Events;

// Absent Host is the standalone-dev default: DependencyInjection falls back
// to MassTransit's in-memory transport instead of requiring a broker just to
// run this service alone.
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string? Host { get; init; }
    public ushort Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
}
