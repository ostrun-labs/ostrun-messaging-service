using NSubstitute;
using OstrunNotificationService.Application.Abstractions;
using OstrunNotificationService.Application.Notifications;
using OstrunNotificationService.Domain.Entities;
using Xunit;

namespace OstrunNotificationService.UnitTests.Notifications;

public class NotificationServiceTests
{
    private readonly ISentEmailRepository _sentEmailRepository = Substitute.For<ISentEmailRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(_sentEmailRepository, _emailSender, _eventPublisher);
    }

    [Fact]
    public async Task HandleUserRegisteredAsync_WhenNotAlreadySent_SendsEmailAndPublishesEvent()
    {
        var userId = Guid.NewGuid();
        _sentEmailRepository.ExistsAsync("Ostrun.Auth.UserRegistered", userId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.HandleUserRegisteredAsync(
            new UserRegisteredNotification(userId, "new@ostrun.dev", DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.Sent);
        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.ToEmail == "new@ostrun.dev"), Arg.Any<CancellationToken>());
        await _sentEmailRepository.Received(1).AddAsync(
            Arg.Is<SentEmail>(e => e.UserId == userId && e.SourceEvent == "Ostrun.Auth.UserRegistered"),
            Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishEmailSentAsync(
            Arg.Is<EmailSentEvent>(e => e.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleUserRegisteredAsync_WhenAlreadySent_IsIdempotentAndSkipsSend()
    {
        var userId = Guid.NewGuid();
        _sentEmailRepository.ExistsAsync("Ostrun.Auth.UserRegistered", userId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.HandleUserRegisteredAsync(
            new UserRegisteredNotification(userId, "duplicate@ostrun.dev", DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.Sent);
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _eventPublisher.DidNotReceive().PublishEmailSentAsync(Arg.Any<EmailSentEvent>(), Arg.Any<CancellationToken>());
    }
}
