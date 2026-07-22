# 🚕 Zedo Go

**Language:** English · [العربية](README.ar.md)

A ride‑hailing (taxi booking) backend API built with **.NET 10** following **Clean Architecture**. It ships with a small static frontend (customer / driver / admin dashboards) and uses **SQLite** by default so it runs with zero database setup.

> Open‑source project · MIT License · © 2026 Rashedx1m

---

## What is Zedo Go?

Zedo Go is the backend for a taxi/ride‑hailing service. It exposes a clean REST API that covers the full ride lifecycle:

- **Accounts & auth** — register, login (JWT), change password, roles (Customer / Driver / Admin).
- **Drivers** — profile, availability (online/offline), live location, nearby search.
- **Ride requests** — create, accept, driver‑arrived, start, complete, cancel, plus nearby‑for‑driver.
- **Pricing** — configurable fare (base + per‑km + per‑minute + minimum), cost estimation.
- **Payments** — automatic fare calculation on completion with company/driver split, earnings & revenue reports.
- **Dashboard** — aggregate stats for an admin view.

## Architecture

The solution follows **Clean Architecture**: dependencies point **inward**, the domain has no dependency on frameworks or the database, and outer layers are swappable.

```
API            → Controllers, Program.cs, JWT/Swagger, DI wiring   (presentation)
Application    → Services, DTOs, interfaces, Result pattern, Mappers (use cases)
Domain         → Entities, Enums, repository interfaces             (core, no dependencies)
Infrastructure → EF Core DbContext, repositories, UnitOfWork, migrations (data / external)
```

Key patterns: **Repository + Unit of Work**, **Result** for explicit success/failure, **DTOs** to decouple the API from entities, and constructor **dependency injection** throughout.

A general, framework‑agnostic explanation lives in [docs/CLEAN_ARCHITECTURE.md](docs/CLEAN_ARCHITECTURE.md).

## Scalability

The design is built to grow without rewrites:

- **Swappable database** — the provider is isolated in `Infrastructure`; moving from SQLite to SQL Server / PostgreSQL / MySQL is a one‑file change (see below).
- **Stateless API + JWT** — horizontally scalable behind a load balancer; no server‑side session.
- **Layer isolation** — business rules live in `Domain`/`Application`, so you can add a mobile BFF, gRPC, or background workers without touching core logic.
- **Unit of Work / transactions** — consistent writes, easy to extend with outbox/events later.
- **Testable core** — `Domain` and `Application` have no infrastructure dependencies, so they unit‑test in isolation.

## Tech stack

| Area | Choice |
|------|--------|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API + Swagger (Swashbuckle) |
| ORM | Entity Framework Core 10 |
| Database (default) | SQLite (file, no server) |
| Auth | JWT Bearer |
| Passwords | BCrypt |

---

## Getting started (for developers)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- No database server required (SQLite is a local file).

### Clone & run
```bash
git clone <your-repo-url>
cd zedo-go-v2
dotnet run --project API
```

The database file (`zedo-go.db`) is **created automatically** and migrations are **applied at startup** — no manual steps.

Then open Swagger:
```
http://localhost:5000
```

### Project structure
```
zedo-go-v2/
├── API/                 # Presentation: controllers, Program.cs, wwwroot (static UI)
├── Application/         # Use cases: services, DTOs, interfaces, Result, Mappers
├── Domain/              # Core: entities, enums, repository interfaces
├── Infrastructure/      # EF Core DbContext, repositories, UnitOfWork, migrations
├── docs/                # Documentation, Postman collection, legacy SQL schema
└── zedo-go.sln
```

### Making changes
- **Add an endpoint:** define the interface in `Application/Interfaces`, implement it in `Application/Services`, register it in `Infrastructure/DependencyInjection.cs`, and expose it from a controller in `API/Controllers`.
- **Add/modify data:** edit entities in `Domain/Entities`, adjust `Infrastructure/Data/AppDbContext.cs`, then create a migration:
  ```bash
  dotnet tool restore
  dotnet ef migrations add <Name> -p Infrastructure -s API
  ```
  Migrations are applied automatically the next time the app starts.

---

## Switching the database (e.g. to MySQL)

Because the provider is isolated in `Infrastructure`, switching databases is a small, well‑defined change.

**1. Swap the EF Core provider package** in `Infrastructure/Infrastructure.csproj` (and `API/API.csproj`):
```diff
- <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.1" />
+ <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="..." />
```
> Note on versions: use a provider that matches your EF Core major version. Pomelo tracks EF Core; if you are on EF Core 10 and a matching Pomelo release is not yet available, either use the official `MySql.EntityFrameworkCore` 10.x provider, or align EF Core to the latest version Pomelo supports.

**2. Change the provider call** in `Infrastructure/DependencyInjection.cs`:
```diff
- options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
+ options.UseMySql(
+     configuration.GetConnectionString("DefaultConnection"),
+     ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection")))
```
(For SQL Server use `UseSqlServer(...)`; for PostgreSQL add `Npgsql.EntityFrameworkCore.PostgreSQL` and use `UseNpgsql(...)`.)

**3. Update the connection string** in `API/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=zedogo;User=root;Password=your_password;"
}
```

**4. Regenerate migrations** (SQLite migrations are provider‑specific):
```bash
# delete the existing Infrastructure/Migrations folder, then:
dotnet ef migrations add InitialCreate -p Infrastructure -s API
dotnet ef database update -p Infrastructure -s API
```

**5. Run** — the app applies migrations at startup:
```bash
dotnet run --project API
```

> Tip: SQLite stores `decimal` as text, so `HasPrecision(...)` is ignored there. On MySQL/SQL Server/PostgreSQL those precisions take effect as real `DECIMAL` columns — no code change needed.

---

## API testing (Postman)

A ready‑to‑use Postman collection is included so a frontend developer can start immediately once the backend is running (locally or deployed):

- Import [`docs/zedo-go.postman_collection.json`](docs/zedo-go.postman_collection.json) into Postman.
- Set the `baseUrl` collection variable (default `http://localhost:5000`).
- Run **Auth → Login** first; the token is captured automatically into the `token` variable and reused by all authorized requests.

---

## Deployment & publishing

> ⚠️ **This stage is intentionally left for a DevOps/deployment specialist.**
> Preparing production deployment (containerization, environment configuration and secrets, a production‑grade database, HTTPS/reverse proxy, CI/CD, logging & monitoring) should be designed and written by an expert for the target environment. Treat the notes here as a starting point, not a production setup.

Minimum hardening before going live:
- Move `Jwt:Key` (and any secrets) out of `appsettings.json` into environment variables / a secrets store, and use a strong key.
- Switch to a server database (see the section above) and disable automatic migrations in production if your process runs migrations separately.
- Configure CORS for your real frontend origin instead of `AllowAll`.

---

## Comparing with the previous version

An earlier, non‑Clean‑Architecture version of this project exists. If you want to see the difference in structure and separation of concerns, compare this repository against that legacy version.

## License

Released under the **MIT License** — see [LICENSE](LICENSE). © 2026 Rashedx1m.
