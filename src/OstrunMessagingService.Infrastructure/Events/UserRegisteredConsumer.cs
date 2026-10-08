using MassTransit;
using OstrunMessagingService.Application.Messaging;

namespace OstrunMessagingService.Infrastructure.Events;

public sealed class UserRegisteredConsumer(MessagingService messagingService) : IConsumer<UserRegistered>
{
    public Task Consume(ConsumeContext<UserRegistered> context) =>
        messagingService.HandleUserRegisteredAsync(
            new UserRegisteredEvent(context.Message.UserId, context.Message.Email, context.Message.RegisteredAt),
            context.CancellationToken);
}
