using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OstrunMessagingService.Application.Abstractions;
using OstrunMessagingService.Application.Messaging;
using OstrunMessagingService.Infrastructure.Events;
using Xunit;

namespace OstrunMessagingService.UnitTests.Events;

public class UserRegisteredConsumerTests
{
    [Fact]
    public async Task Consume_RoutesMessageToMessagingService()
    {
        var sentEmailRepository = Substitute.For<ISentEmailRepository>();
        var emailSender = Substitute.For<IEmailSender>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        sentEmailRepository.ExistsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await using var provider = new ServiceCollection()
            .AddSingleton(sentEmailRepository)
            .AddSingleton(emailSender)
            .AddSingleton(eventPublisher)
            .AddScoped<MessagingService>()
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
