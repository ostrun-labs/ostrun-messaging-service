using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OstrunNotificationService.Application.Abstractions;
using OstrunNotificationService.Infrastructure.Persistence;

namespace OstrunNotificationService.IntegrationTests;

[Collection(NotificationCollection.Name)]
public class RetryTests(NotificationApiFactory factory)
{
    [Fact]
    public async Task SmtpDown_ThenBack_SendsTheEmailOnceAfterRetrying()
    {
        var userId = Guid.NewGuid();
        var email = $"{userId}@ostrun.dev";
        var errorsBefore = await factory.ErrorQueueCountAsync();
        factory.SmtpFaults.FailNext(email, times: 2);
        await using var emailSent = await factory.ListenForEmailSentAsync(userId);

        await factory.PublishUserRegisteredAsync(userId, email);

        Assert.Single(await emailSent.WaitForAsync(1));
        Assert.Single(await factory.WaitForMessagesToAsync(email));
        Assert.Equal(3, factory.SmtpFaults.Attempts(email));
        Assert.Equal(1, await SentEmailCount(userId));
        Assert.Equal(errorsBefore, await factory.ErrorQueueCountAsync());
    }

    [Fact]
    public async Task InvalidRecipient_IsNotRetried_AndGoesToTheErrorQueue()
    {
        var userId = Guid.NewGuid();
        var email = $"not-an-address-{userId}@@ostrun.dev";
        var errorsBefore = await factory.ErrorQueueCountAsync();

        await factory.PublishUserRegisteredAsync(userId, email);

        await factory.WaitForErrorQueueCountAsync(errorsBefore + 1);
        Assert.Equal(1, factory.SmtpFaults.Attempts(email));
        Assert.Equal(0, await SentEmailCount(userId));
    }

    private async Task<int> SentEmailCount(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        return await db.SentEmails.CountAsync(e => e.UserId == userId);
    }
}

public sealed class SmtpFaults
{
    private readonly ConcurrentDictionary<string, int> _failuresLeft = new();
    private readonly ConcurrentDictionary<string, int> _attempts = new();

    public void FailNext(string recipient, int times) => _failuresLeft[recipient] = times;

    public int Attempts(string recipient) => _attempts.GetValueOrDefault(recipient);

    // Records an attempt and says whether it should fail.
    public bool RecordAttempt(string recipient)
    {
        _attempts.AddOrUpdate(recipient, 1, (_, count) => count + 1);
        var shouldFail = false;
        _failuresLeft.AddOrUpdate(recipient, 0, (_, left) =>
        {
            shouldFail = left > 0;
            return Math.Max(left - 1, 0);
        });
        return shouldFail;
    }
}

// Wraps the real SMTP sender and fails while SmtpFaults says so, as if the
// SMTP server refused the connection.
public sealed class FaultInjectingEmailSender(IEmailSender inner, SmtpFaults faults) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        faults.RecordAttempt(message.ToEmail)
            ? throw new SocketException((int)SocketError.ConnectionRefused)
            : inner.SendAsync(message, cancellationToken);
}
