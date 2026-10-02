using OstrunNotificationService.Application.Abstractions;
using OstrunNotificationService.Domain.Entities;

namespace OstrunNotificationService.Application.Notifications;

public sealed class NotificationService(
    ISentEmailRepository sentEmailRepository,
    IEmailSender emailSender,
    IEventPublisher eventPublisher)
{
    private const string UserRegisteredSourceEvent = "Ostrun.Auth.UserRegistered";

    public async Task<SendWelcomeEmailResult> HandleUserRegisteredAsync(
        UserRegisteredNotification notification, CancellationToken cancellationToken)
    {
        // The broker may redeliver a message; skip if we already handled this event.
        if (await sentEmailRepository.ExistsAsync(UserRegisteredSourceEvent, notification.UserId, cancellationToken))
        {
            return new SendWelcomeEmailResult(Sent: false);
        }

        var message = new EmailMessage(
            notification.Email,
            "Welcome to Ostrun",
            $"<p>Hi,</p><p>Your Ostrun account has been created with the address {notification.Email}.</p>");

        await emailSender.SendAsync(message, cancellationToken);

        var sentAt = DateTime.UtcNow;
        await sentEmailRepository.AddAsync(
            new SentEmail(Guid.NewGuid(), UserRegisteredSourceEvent, notification.UserId, notification.Email, sentAt),
            cancellationToken);

        await eventPublisher.PublishEmailSentAsync(
            new EmailSentEvent(notification.UserId, notification.Email, UserRegisteredSourceEvent, sentAt),
            cancellationToken);

        return new SendWelcomeEmailResult(Sent: true);
    }
}
