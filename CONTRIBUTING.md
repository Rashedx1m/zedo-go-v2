# Contributing to Zedo Go

Thanks for your interest in contributing! This is an open‑source project and
contributions are welcome. / شكراً لاهتمامك بالمساهمة! المشروع مفتوح المصدر
والمساهمات مرحّب بها.

## Getting started

1. Fork the repository and clone your fork.
2. Make sure you have the [.NET 10 SDK](https://dotnet.microsoft.com/download).
3. Build and run:
   ```bash
   dotnet build zedo-go.sln
   dotnet run --project API
   ```
   The SQLite database is created automatically on first run.

## Development workflow

1. Create a branch from `main`:
   ```bash
   git checkout -b feature/short-description
   ```
2. Make your change, keeping the Clean Architecture boundaries:
   - **Domain** stays free of any framework/database dependency.
   - Business logic lives in **Application** (services) behind interfaces.
   - Data access and external concerns live in **Infrastructure**.
   - Controllers in **API** stay thin — they call services and return results.
3. If you change entities or the model, add a migration:
   ```bash
   dotnet tool restore
   dotnet ef migrations add <Name> -p Infrastructure -s API
   ```
4. Ensure the solution builds cleanly:
   ```bash
   dotnet build zedo-go.sln -c Release
   ```
5. Commit with a clear message and open a Pull Request against `main`.

## Guidelines

- Keep changes focused; one logical change per PR.
- Match the existing code style and naming.
- Do not commit build artifacts or local databases (`bin/`, `obj/`, `*.db`) —
  they are already in `.gitignore`.
- Do not commit secrets. Keep real keys out of `appsettings.json`.

## Reporting issues

Open a GitHub issue with a clear description, steps to reproduce, and the
expected vs. actual behavior.
