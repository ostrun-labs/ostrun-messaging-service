using Microsoft.EntityFrameworkCore;
using OstrunNotificationService.Application.Abstractions;
using OstrunNotificationService.Domain.Entities;

namespace OstrunNotificationService.Infrastructure.Persistence;

public sealed class SentEmailRepository(NotificationDbContext dbContext) : ISentEmailRepository
{
    public Task<bool> ExistsAsync(string sourceEvent, Guid userId, CancellationToken cancellationToken) =>
        dbContext.SentEmails.AnyAsync(e => e.SourceEvent == sourceEvent && e.UserId == userId, cancellationToken);

    public async Task AddAsync(SentEmail sentEmail, CancellationToken cancellationToken)
    {
        dbContext.SentEmails.Add(sentEmail);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
