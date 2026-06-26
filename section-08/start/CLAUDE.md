# CLAUDE.md

Expense Tracker web app.

## Stack

- .NET 10 Minimal API (`api/ExpenseTracker.Api`)
- React 19 / Vite SPA (`web/`) 
- PostgreSQL 18 via Docker.

## Common commands

```sh
docker compose up -d                                            # Postgres on :5432
dotnet ef database update --project api/ExpenseTracker.Api      # apply migrations
dotnet run --project api/ExpenseTracker.Api                     # API on :5057
npm --prefix web install && npm --prefix web run dev            # SPA on :5173

dotnet build                                                    # ExpenseTracker.slnx
dotnet test                                                     # Docker must be running
dotnet test --filter "FullyQualifiedName~ExpenseApiTests.Should_return_201"
npm --prefix web run typecheck
npm --prefix web run build

dotnet ef migrations add <Name> --project api/ExpenseTracker.Api
```

Warnings fail the build (`TreatWarningsAsErrors=true`). Fix them, don't suppress.

## Rules

### API

- Add new routes as a route group + static handlers + a `MapXxxEndpoints` extension called from `Program.cs`. Don't put handlers inline in `Program.cs`.
- Validate inputs by hand in the handler and return `Results.ValidationProblem`. Don't add FluentValidation or DataAnnotations.
- Put wire types in `Contracts/` as records. Don't return `Domain/Expense` from endpoints.
- Let Postgres set `CreatedAt` via the `now() at time zone 'utc'` default. Don't assign it in C#.

### Tests

- `PostgresApiFactory` is registered as an `AssemblyFixture` in `AssemblyFixtures.cs`. Inject it via constructor on test classes; don't add `IClassFixture<PostgresApiFactory>` or register it again.
- Tests use Testcontainers Postgres, not the `docker compose` instance. Don't reuse the dev connection string in tests.

### Web

- Use bare `/api/...` paths in the SPA. Vite proxies them to `:5057`; don't hardcode the API origin.
- Serialize/parse `DateOnly` as `YYYY-MM-DD` strings on both sides. Don't use `Date` objects on the wire.

### Layout

- Project folders are `Product.Component` PascalCase (`ExpenseTracker.Api`, `ExpenseTracker.Tests`) inside generic buckets (`api/`, `tests/`).
- Solution file is `.slnx` (XML). Don't generate or commit a `.sln`.

# Domain

@docs/domain-glossary.md