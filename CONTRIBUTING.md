# Contributing

Thanks for your interest in improving the Ostrun Messaging service.

## Before you start

For anything larger than a small fix, open an issue first to discuss the change. This service is part of the [Ostrun](https://github.com/ostrun-labs/ostrun) catalog, so changes to the manifest (`ostrun.manifest.json`) or the event contracts (`contracts/`) affect other services and need agreement before implementation.

## Workflow

1. Fork the repository and create a branch from `main` (`feat/<name>`, `fix/<name>`, `chore/<name>` or `docs/<name>`).
2. Make your change. Keep each pull request focused on one thing.
3. Run the build and tests locally:

   ```bash
   dotnet build
   dotnet test
   ```

4. Open a pull request against `main` describing what changed and why.

CI runs the build and tests on every pull request; it must pass before merging.

## Guidelines

- Follow the existing layering (Domain, Application, Infrastructure, Api): the Application layer depends only on abstractions, never on Infrastructure.
- Add or update unit tests for behavior changes.
- If you add a configuration variable, document it in `ostrun.manifest.json` and `.env.example`.
- Changing a published event's shape requires a new contract version (e.g. `EmailSent.v2.json`) rather than editing the existing one.
- Write commit messages and code comments in English.

## License

By contributing, you agree that your contributions are licensed under the [Apache License 2.0](LICENSE).
