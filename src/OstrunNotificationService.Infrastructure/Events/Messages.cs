namespace OstrunNotificationService.Infrastructure.Events;

// Local message shapes for the events this service publishes/consumes. Kept
// local rather than in a shared contracts package on purpose: a future
// Node/other stack service must be able to interop without a C# dependency.
// RabbitMQ binding is forced to the Ostrun contract name in
// DependencyInjection.cs, not derived from these CLR type names.

// Consumed: shape mirrors ostrun/contracts/ostrun/auth/UserRegistered.v1.json.
public sealed record UserRegistered
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required DateTime RegisteredAt { get; init; }
}

// Published: shape mirrors contracts/ostrun/notification/EmailSent.v1.json.
public sealed record EmailSent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string SourceEvent { get; init; }
    public required DateTime SentAt { get; init; }
}
