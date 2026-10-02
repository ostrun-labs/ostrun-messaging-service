using Microsoft.EntityFrameworkCore;
using OstrunNotificationService.Domain.Entities;

namespace OstrunNotificationService.Infrastructure.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<SentEmail> SentEmails => Set<SentEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SentEmail>(entity =>
        {
            entity.ToTable("sent_emails");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceEvent).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(320);
            entity.Property(e => e.SentAt).IsRequired();
            entity.HasIndex(e => new { e.SourceEvent, e.UserId }).IsUnique();
        });
    }
}
