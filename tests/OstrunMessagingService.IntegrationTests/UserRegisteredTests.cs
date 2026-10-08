using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OstrunMessagingService.Infrastructure.Persistence;

namespace OstrunMessagingService.IntegrationTests;

[Collection(MessagingCollection.Name)]
public class UserRegisteredTests(MessagingApiFactory factory)
{
    [Fact]
    public async Task UserRegistered_SendsWelcomeEmail_RecordsIt_AndPublishesEmailSent()
    {
        var userId = Guid.NewGuid();
        var email = $"{userId}@ostrun.dev";
        await using var emailSent = await factory.ListenForEmailSentAsync(userId);

        await factory.PublishUserRegisteredAsync(userId, email);
        await factory.WaitUntilConsumedAsync(userId, times: 1);

        var message = Assert.Single(await factory.WaitForMessagesToAsync(email));
        Assert.Equal("Welcome to Ostrun", message.Subject);
        Assert.Equal("no-reply@ostrun.dev", message.From.Address);
        Assert.Equal("Ostrun", message.From.Name);

        Assert.Equal(1, await SentEmailCount(userId));

        var published = Assert.Single(await emailSent.DrainAsync());
        Assert.Equal(["email", "sentAt", "sourceEvent", "userId"], published.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(userId, published.GetProperty("userId").GetGuid());
        Assert.Equal(email, published.GetProperty("email").GetString());
        Assert.Equal("Ostrun.Auth.UserRegistered", published.GetProperty("sourceEvent").GetString());
    }

    [Fact]
    public async Task UserRegistered_DeliveredTwice_SendsOnlyOnce()
    {
        var userId = Guid.NewGuid();
        var email = $"{userId}@ostrun.dev";
        await using var emailSent = await factory.ListenForEmailSentAsync(userId);

        await factory.PublishUserRegisteredAsync(userId, email);
        await factory.WaitUntilConsumedAsync(userId, times: 1);
        await factory.PublishUserRegisteredAsync(userId, email);
        await factory.WaitUntilConsumedAsync(userId, times: 2);

        Assert.Single(await factory.WaitForMessagesToAsync(email));
        Assert.Equal(1, await SentEmailCount(userId));
        Assert.Single(await emailSent.DrainAsync());
    }

    private async Task<int> SentEmailCount(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
        return await db.SentEmails.CountAsync(e => e.UserId == userId);
    }
}
