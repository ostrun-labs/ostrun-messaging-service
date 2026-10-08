using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OstrunMessagingService.Application.Abstractions;
using OstrunMessagingService.Infrastructure.Email;
using OstrunMessagingService.Infrastructure.Events;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace OstrunMessagingService.IntegrationTests;

// Runs the real service in memory against throwaway Postgres, RabbitMQ and
// Mailpit containers. Shared by every test in the "messaging" collection;
// tests use unique user ids and emails instead of resetting state.
public sealed class MessagingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string RabbitMqUser = "ostrun";
    private const string RabbitMqPassword = "ostrun";
    private const ushort MailpitSmtpPort = 1025;
    private const ushort MailpitApiPort = 8025;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-alpine")
        .WithUsername(RabbitMqUser)
        .WithPassword(RabbitMqPassword)
        .Build();

    private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:latest")
        .WithPortBinding(MailpitSmtpPort, true)
        .WithPortBinding(MailpitApiPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(MailpitApiPort).ForPath("/livez")))
        .Build();

    private readonly ConsumedCounter _consumed = new();

    // Lets a test make SMTP sends fail for a given recipient.
    public SmtpFaults SmtpFaults { get; } = new();
    private HttpClient _mailpitApi = null!;
    private IConnection _rabbitMqConnection = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync(), _mailpit.StartAsync());

        // Environment variables rather than WebApplicationFactory settings:
        // Program.cs reads configuration before building the host, where
        // factory settings aren't applied yet.
        Environment.SetEnvironmentVariable("ConnectionStrings__Messaging", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitMq.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", _rabbitMq.GetMappedPublicPort(5672).ToString());
        Environment.SetEnvironmentVariable("RabbitMq__Username", RabbitMqUser);
        Environment.SetEnvironmentVariable("RabbitMq__Password", RabbitMqPassword);
        Environment.SetEnvironmentVariable("Smtp__Host", _mailpit.Hostname);
        Environment.SetEnvironmentVariable("Smtp__Port", _mailpit.GetMappedPublicPort(MailpitSmtpPort).ToString());
        Environment.SetEnvironmentVariable("Smtp__UseStartTls", "false");
        Environment.SetEnvironmentVariable("Smtp__FromAddress", "no-reply@ostrun.dev");

        _mailpitApi = new HttpClient
        {
            BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(MailpitApiPort)}"),
        };
        _rabbitMqConnection = await new ConnectionFactory { Uri = new Uri(_rabbitMq.GetConnectionString()) }
            .CreateConnectionAsync();

        // Start the host now so the bus has declared its exchanges and queues
        // before any test publishes.
        Services.GetRequiredService<IBus>().ConnectConsumeMessageObserver(_consumed);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.Configure<MassTransitHostOptions>(options => options.WaitUntilStarted = true);
            services.AddScoped<SmtpEmailSender>();
            services.AddScoped<IEmailSender>(sp => new FaultInjectingEmailSender(sp.GetRequiredService<SmtpEmailSender>(), SmtpFaults));
        });

    // Publishes the event the way ostrun-auth-service does: raw JSON on the
    // Ostrun.Auth.UserRegistered exchange, no MassTransit envelope.
    public async Task PublishUserRegisteredAsync(Guid userId, string email)
    {
        await using var channel = await _rabbitMqConnection.CreateChannelAsync();
        var body = JsonSerializer.SerializeToUtf8Bytes(new { userId, email, registeredAt = DateTime.UtcNow });
        await channel.BasicPublishAsync(
            "Ostrun.Auth.UserRegistered",
            routingKey: string.Empty,
            mandatory: false,
            new BasicProperties { ContentType = "application/json" },
            body);
    }

    // Waits until the service has finished handling `times` UserRegistered
    // messages for this user, whether or not a message led to a send.
    public async Task WaitUntilConsumedAsync(Guid userId, int times)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (_consumed.Count(userId) < times)
        {
            await Task.Delay(100, cts.Token);
        }
    }

    // Binds a temporary queue to the EmailSent exchange and keeps the events
    // for this user. Call before publishing.
    public async Task<EmailSentListener> ListenForEmailSentAsync(Guid userId)
    {
        var channel = await _rabbitMqConnection.CreateChannelAsync();
        const string exchange = "Ostrun.Messaging.EmailSent";
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true);
        var queue = await channel.QueueDeclareAsync(exclusive: true);
        await channel.QueueBindAsync(queue.QueueName, exchange, routingKey: string.Empty);
        return new EmailSentListener(channel, queue.QueueName, userId);
    }

    // Messages MassTransit moved to the consumer's error queue, i.e. given up on.
    public async Task<uint> ErrorQueueCountAsync()
    {
        await using var channel = await _rabbitMqConnection.CreateChannelAsync();
        try
        {
            return (await channel.QueueDeclarePassiveAsync("user-registered_error")).MessageCount;
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            return 0; // MassTransit creates the error queue on the first failure.
        }
    }

    public async Task WaitForErrorQueueCountAsync(uint count)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (await ErrorQueueCountAsync() < count)
        {
            await Task.Delay(100, cts.Token);
        }
    }

    public async Task<IReadOnlyList<MailpitMessage>> MessagesToAsync(string email)
    {
        var result = await _mailpitApi.GetFromJsonAsync<MailpitSearchResult>(
            $"/api/v1/search?query={Uri.EscapeDataString($"to:\"{email}\"")}");
        return result!.Messages;
    }

    public async Task<IReadOnlyList<MailpitMessage>> WaitForMessagesToAsync(string email)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (true)
        {
            var messages = await MessagesToAsync(email);
            if (messages.Count > 0)
            {
                return messages;
            }

            await Task.Delay(100, cts.Token);
        }
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _rabbitMqConnection.DisposeAsync();
        _mailpitApi.Dispose();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask(), _mailpit.DisposeAsync().AsTask());
    }

    private sealed class ConsumedCounter : IConsumeMessageObserver<UserRegistered>
    {
        private readonly ConcurrentDictionary<Guid, int> _counts = new();

        public int Count(Guid userId) => _counts.GetValueOrDefault(userId);

        public Task PreConsume(ConsumeContext<UserRegistered> context) => Task.CompletedTask;

        public Task PostConsume(ConsumeContext<UserRegistered> context) => Increment(context);

        public Task ConsumeFault(ConsumeContext<UserRegistered> context, Exception exception) => Increment(context);

        private Task Increment(ConsumeContext<UserRegistered> context)
        {
            _counts.AddOrUpdate(context.Message.UserId, 1, (_, count) => count + 1);
            return Task.CompletedTask;
        }
    }
}

public sealed class EmailSentListener(IChannel channel, string queue, Guid userId) : IAsyncDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly List<JsonElement> _received = [];

    // Every Ostrun.Messaging.EmailSent for this user since the listener was
    // created, as raw JSON. Events for other users are dropped: an earlier
    // test's event can still arrive while this one runs.
    public async Task<IReadOnlyList<JsonElement>> DrainAsync()
    {
        while (await channel.BasicGetAsync(queue, autoAck: true) is { } result)
        {
            var message = JsonDocument.Parse(result.Body).RootElement.Clone();
            if (message.GetProperty("userId").GetGuid() == userId)
            {
                _received.Add(message);
            }
        }

        return _received;
    }

    // EmailSent is the last thing the service does for an event, so once it
    // arrives the email has gone out and the row is saved.
    public async Task<IReadOnlyList<JsonElement>> WaitForAsync(int count)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while ((await DrainAsync()).Count < count)
        {
            await Task.Delay(100, cts.Token);
        }

        return _received;
    }

    public async ValueTask DisposeAsync() => await channel.DisposeAsync();
}

public sealed record MailpitSearchResult(IReadOnlyList<MailpitMessage> Messages);

public sealed record MailpitMessage(MailpitAddress From, IReadOnlyList<MailpitAddress> To, string Subject);

public sealed record MailpitAddress(string Name, string Address);

[CollectionDefinition(Name)]
public sealed class MessagingCollection : ICollectionFixture<MessagingApiFactory>
{
    public const string Name = "messaging";
}
