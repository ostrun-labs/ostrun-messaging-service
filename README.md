# ostrun-messaging-service

Reference messaging service for [Ostrun](https://github.com/ostrun-labs/ostrun), provider `Ostrun`, service `messaging`.

## Status

Event contract and a .NET 8 service (event consumption / SMTP sending) are written. This service is developed separately from Ostrun's core repo. Ostrun only references it (through its manifest, `repository` and optionally `registry`), and the central catalog points here instead of hosting its code.

## Role

- Sends email notifications triggered by other services' events (e.g. a welcome email on registration)
- Consumes `Ostrun.Auth.UserRegistered`, publishes `Ostrun.Messaging.EmailSent`
- Remains an independent, replaceable service

## Independence

- Its own database and schema (PostgreSQL), used for send idempotence
- Its own `Dockerfile`, deployable standalone
- Communication only via API or events, never a direct call into another service

## Receiving events

Events are received via RabbitMQ (MassTransit). `UserRegisteredConsumer` subscribes to the `Ostrun.Auth.UserRegistered` exchange published by `ostrun-auth-service`, deserializes the payload as raw JSON (no MassTransit envelope, no dependency on a shared C# type; see `src/OstrunMessagingService.Infrastructure/DependencyInjection.cs`), and follows the JSON contract published by the emitting service:

- Consumes `Ostrun.Auth.UserRegistered` (`{ userId, email, registeredAt }`), sends the welcome email, publishes `Ostrun.Messaging.EmailSent` (contract: `contracts/ostrun/messaging/EmailSent.v1.json`). Idempotent: a given `userId` triggers the send only once.
- A failed send (SMTP or database unavailable) is retried 5 times, backing off from 1s up to 30s, before the message goes to the `user-registered_error` queue. An invalid recipient address goes there right away, since retrying can't fix it.
- Without `RabbitMq__Host` set in configuration, the service falls back to MassTransit's in-memory transport. No broker is required to run it standalone.

## Architecture

Light Clean Architecture in 4 layers:

- `src/OstrunMessagingService.Domain`: the `SentEmail` entity (tracking/idempotence), no dependencies
- `src/OstrunMessagingService.Application`: incoming event processing use cases, interfaces (email sender, repository, events)
- `src/OstrunMessagingService.Infrastructure`: EF Core + Npgsql, SMTP sending (MailKit), event publishing/consuming via MassTransit + RabbitMQ
- `src/OstrunMessagingService.Api`: .NET 8 Minimal APIs (`/health`)
- `tests/OstrunMessagingService.UnitTests`: unit tests for the Application layer, the MassTransit consumer and the published event contract
- `tests/OstrunMessagingService.IntegrationTests`: runs the real service against throwaway Postgres, RabbitMQ and Mailpit containers (Testcontainers), publishes `Ostrun.Auth.UserRegistered` and checks the email, the stored row and the published `EmailSent`

## Running locally

```bash
cp .env.example .env
docker compose up --build
```

The API listens on `http://localhost:8082` (`/health`). EF Core migrations apply automatically on startup. Emails sent locally are caught by Mailpit, viewable at `http://localhost:8025`.

## Development

```bash
dotnet build
dotnet test   # integration tests need Docker running
dotnet tool run dotnet-ef migrations add <Name> --project src/OstrunMessagingService.Infrastructure --startup-project src/OstrunMessagingService.Api --output-dir Persistence/Migrations
```
