using MassTransit;
using OstrunNotificationService.Application.Notifications;

namespace OstrunNotificationService.Infrastructure.Events;

public sealed class UserRegisteredConsumer(NotificationService notificationService) : IConsumer<UserRegistered>
{
    public Task Consume(ConsumeContext<UserRegistered> context) =>
        notificationService.HandleUserRegisteredAsync(
            new UserRegisteredNotification(context.Message.UserId, context.Message.Email, context.Message.RegisteredAt),
            context.CancellationToken);
}
