using OstrunMessagingService.Application.Abstractions;
using OstrunMessagingService.Domain.Entities;

namespace OstrunMessagingService.Application.Messaging;

public sealed class MessagingService(
    ISentEmailRepository sentEmailRepository,
    IEmailSender emailSender,
    IEventPublisher eventPublisher)
{
    private const string UserRegisteredSourceEvent = "Ostrun.Auth.UserRegistered";

    public async Task<SendWelcomeEmailResult> HandleUserRegisteredAsync(
        UserRegisteredEvent userRegistered, CancellationToken cancellationToken)
    {
        // The broker may redeliver a message; skip if we already handled this event.
        if (await sentEmailRepository.ExistsAsync(UserRegisteredSourceEvent, userRegistered.UserId, cancellationToken))
        {
            return new SendWelcomeEmailResult(Sent: false);
        }

        var message = new EmailMessage(
            userRegistered.Email,
            "Welcome to Ostrun",
            $"<p>Hi,</p><p>Your Ostrun account has been created with the address {userRegistered.Email}.</p>");

        await emailSender.SendAsync(message, cancellationToken);

        var sentAt = DateTime.UtcNow;
        await sentEmailRepository.AddAsync(
            new SentEmail(Guid.NewGuid(), UserRegisteredSourceEvent, userRegistered.UserId, userRegistered.Email, sentAt),
            cancellationToken);

        await eventPublisher.PublishEmailSentAsync(
            new EmailSentEvent(userRegistered.UserId, userRegistered.Email, UserRegisteredSourceEvent, sentAt),
            cancellationToken);

        return new SendWelcomeEmailResult(Sent: true);
    }
}
