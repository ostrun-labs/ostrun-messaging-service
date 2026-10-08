using Microsoft.EntityFrameworkCore;
using OstrunMessagingService.Application.Abstractions;
using OstrunMessagingService.Domain.Entities;

namespace OstrunMessagingService.Infrastructure.Persistence;

public sealed class SentEmailRepository(MessagingDbContext dbContext) : ISentEmailRepository
{
    public Task<bool> ExistsAsync(string sourceEvent, Guid userId, CancellationToken cancellationToken) =>
        dbContext.SentEmails.AnyAsync(e => e.SourceEvent == sourceEvent && e.UserId == userId, cancellationToken);

    public async Task AddAsync(SentEmail sentEmail, CancellationToken cancellationToken)
    {
        dbContext.SentEmails.Add(sentEmail);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
