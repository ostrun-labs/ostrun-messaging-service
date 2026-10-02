using MassTransit;
using OstrunNotificationService.Application.Abstractions;

namespace OstrunNotificationService.Infrastructure.Events;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishEmailSentAsync(EmailSentEvent emailSent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new EmailSent
            {
                UserId = emailSent.UserId,
                Email = emailSent.Email,
                SourceEvent = emailSent.SourceEvent,
                SentAt = emailSent.SentAtUtc,
            },
            cancellationToken);
}
