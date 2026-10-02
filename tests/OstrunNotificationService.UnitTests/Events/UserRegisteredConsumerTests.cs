using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OstrunNotificationService.Application.Abstractions;
using OstrunNotificationService.Application.Notifications;
using OstrunNotificationService.Infrastructure.Events;
using Xunit;

namespace OstrunNotificationService.UnitTests.Events;

public class UserRegisteredConsumerTests
{
    [Fact]
    public async Task Consume_RoutesMessageToNotificationService()
    {
        var sentEmailRepository = Substitute.For<ISentEmailRepository>();
        var emailSender = Substitute.For<IEmailSender>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        sentEmailRepository.ExistsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await using var provider = new ServiceCollection()
            .AddSingleton(sentEmailRepository)
            .AddSingleton(emailSender)
            .AddSingleton(eventPublisher)
            .AddScoped<NotificationService>()
            .AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var userId = Guid.NewGuid();
        await harness.Bus.Publish(new UserRegistered
        {
            UserId = userId,
            Email = "new@ostrun.dev",
            RegisteredAt = DateTime.UtcNow,
        });

        Assert.True(await harness.Consumed.Any<UserRegistered>());
        await emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToEmail == "new@ostrun.dev"), Arg.Any<CancellationToken>());
    }
}
