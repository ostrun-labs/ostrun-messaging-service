namespace OstrunNotificationService.Application.Notifications;

// Shape mirrors the Ostrun.Auth.UserRegistered event contract
// (ostrun-auth-service/contracts/ostrun/auth/UserRegistered.v1.json).
public sealed record UserRegisteredNotification(Guid UserId, string Email, DateTime RegisteredAt);
