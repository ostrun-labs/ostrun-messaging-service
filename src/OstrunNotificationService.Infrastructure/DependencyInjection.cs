using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OstrunNotificationService.Application.Abstractions;
using OstrunNotificationService.Application.Notifications;
using OstrunNotificationService.Infrastructure.Email;
using OstrunNotificationService.Infrastructure.Events;
using OstrunNotificationService.Infrastructure.Persistence;

namespace OstrunNotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Notification")));

        services.AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection(SmtpSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var rabbitMq = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<UserRegisteredConsumer>();

            if (!string.IsNullOrWhiteSpace(rabbitMq.Host))
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMq.Host, rabbitMq.Port, rabbitMq.VirtualHost, h =>
                    {
                        h.Username(rabbitMq.Username);
                        h.Password(rabbitMq.Password);
                    });

                    // Plain JSON on the wire, message-type check disabled.
                    // See ostrun-auth-service's DependencyInjection.cs for why.
                    cfg.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType | RawSerializerOptions.AddTransportHeaders | RawSerializerOptions.CopyHeaders);

                    // Same forced entity names as the publishing side (Auth).
                    // See ostrun-auth-service/.../DependencyInjection.cs.
                    cfg.Message<UserRegistered>(m => m.SetEntityName("Ostrun.Auth.UserRegistered"));
                    cfg.Message<EmailSent>(m => m.SetEntityName("Ostrun.Notification.EmailSent"));

                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // Standalone dev default: no RabbitMq:Host configured, so this
                // service still runs alone with no broker.
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            }
        });

        services.AddScoped<ISentEmailRepository, SentEmailRepository>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
        services.AddScoped<NotificationService>();

        return services;
    }
}
