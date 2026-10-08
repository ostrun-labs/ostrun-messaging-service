using MassTransit;
using OstrunMessagingService.Application.Abstractions;

namespace OstrunMessagingService.Infrastructure.Events;

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
