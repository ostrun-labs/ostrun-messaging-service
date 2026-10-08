namespace OstrunMessagingService.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailMessage(string ToEmail, string Subject, string HtmlBody);
