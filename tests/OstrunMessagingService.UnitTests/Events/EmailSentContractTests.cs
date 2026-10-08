using System.Text.Json;
using MassTransit.Serialization;
using OstrunMessagingService.Infrastructure.Events;
using Xunit;

namespace OstrunMessagingService.UnitTests.Events;

public class EmailSentContractTests
{
    [Fact]
    public void EmailSent_SerializesWithContractPropertyNames()
    {
        // Property names required by contracts/ostrun/messaging/EmailSent.v1.json
        // (which also sets additionalProperties: false).
        string[] contractProperties = ["userId", "email", "sourceEvent", "sentAt"];

        var json = JsonSerializer.Serialize(
            new EmailSent
            {
                UserId = Guid.NewGuid(),
                Email = "new@ostrun.dev",
                SourceEvent = "Ostrun.Auth.UserRegistered",
                SentAt = DateTime.UtcNow,
            },
            SystemTextJsonMessageSerializer.Options);

        var properties = JsonDocument.Parse(json).RootElement.EnumerateObject().Select(p => p.Name);
        Assert.Equal(contractProperties.Order(), properties.Order());
    }
}
