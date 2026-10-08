namespace OstrunMessagingService.Domain.Entities;

// One row per (SourceEvent, UserId): the unique index on that pair is what makes
// event handling idempotent when the broker redelivers a message.
public sealed class SentEmail
{
    public Guid Id { get; private set; }
    public string SourceEvent { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; }

    private SentEmail()
    {
    }

    public SentEmail(Guid id, string sourceEvent, Guid userId, string recipientEmail, DateTime sentAt)
    {
        Id = id;
        SourceEvent = sourceEvent;
        UserId = userId;
        RecipientEmail = recipientEmail;
        SentAt = sentAt;
    }
}
