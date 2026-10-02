# ostrun-notification-service

Reference Notification service for [Ostrun](https://github.com/ostrun-labs/ostrun), provider `Ostrun`, service `notification`.

## Status

Repository setup only. The .NET 8 service is not written yet. This service is developed separately from Ostrun's core repo. Ostrun only references it (through its manifest, `repository` and optionally `registry`), and the central catalog points here instead of hosting its code.

## Role

- Sends email notifications triggered by other services' events (e.g. a welcome email on registration)
- Consumes `Ostrun.Auth.UserRegistered`, publishes `Ostrun.Notification.EmailSent`
- Remains an independent, replaceable service

## Independence

- Its own database and schema (PostgreSQL), used for send idempotence
- Its own `Dockerfile`, deployable standalone
- Communication only via API or events, never a direct call into another service
