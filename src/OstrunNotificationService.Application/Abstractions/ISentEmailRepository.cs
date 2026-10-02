using OstrunNotificationService.Domain.Entities;

namespace OstrunNotificationService.Application.Abstractions;

public interface ISentEmailRepository
{
    Task<bool> ExistsAsync(string sourceEvent, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(SentEmail sentEmail, CancellationToken cancellationToken);
}
