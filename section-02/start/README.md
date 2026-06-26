# Expense Tracker

A small expense tracker with a .NET Minimal API backend, a React + Vite frontend, and PostgreSQL for storage. This is the Section 02 starter snapshot for the Dometrain course **"Getting Started with Claude Code"** — the running example used through the rest of the course.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (LTS)
- [Node.js 24](https://nodejs.org/) (LTS) — bundles `npm`
- [Docker](https://docs.docker.com/get-docker/) (Desktop or Engine)

## Quick start

```sh
git clone <repo-url> && cd dometrain-claude-code-getting-started/section-02/start
docker compose up -d
dotnet ef database update --project api/ExpenseTracker.Api
dotnet run --project api/ExpenseTracker.Api
npm --prefix web install && npm --prefix web run dev
```

Run the last two commands in separate terminals so the API and the web dev server stay up. The first `dotnet ef` invocation auto-restores the local `dotnet-ef` tool from `.config/dotnet-tools.json`.

## Tests

```sh
dotnet test
```

The integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a Postgres container per test class, so Docker must be running. They do not share state with the database started by `docker compose`.

## Where things live

| Service | URL | Notes |
| --- | --- | --- |
| API | http://localhost:5057 | Endpoints: `POST /api/expenses`, `GET /api/expenses` |
| Web | http://localhost:5173 | Vite dev server; proxies `/api` to the API |
| Postgres | localhost:5432 | Database `expenses`, user `expenses`, password `expenses` (demo credentials, see `appsettings.Development.json`) |
