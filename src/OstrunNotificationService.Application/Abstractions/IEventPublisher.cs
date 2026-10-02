namespace OstrunNotificationService.Application.Abstractions;

// Contract kept broker-agnostic on purpose: the Infrastructure implementation
// (MassTransitEventPublisher) can be swapped for a different transport
// without touching Application or Api.
public interface IEventPublisher
{
    Task PublishEmailSentAsync(EmailSentEvent emailSent, CancellationToken cancellationToken);
}

public sealed record EmailSentEvent(Guid UserId, string Email, string SourceEvent, DateTime SentAtUtc);
