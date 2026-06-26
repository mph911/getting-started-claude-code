# Getting Started with Claude Code

Welcome to the ["Getting Started with Claude Code"](https://dometrain.com/course/getting-started-claude-code/?ref=dometrain-github) course on Dometrain!

This course teaches you how to use Claude Code effectively as an AI-powered coding assistant. You'll learn the fundamentals of working with Claude Code, how to guide it with context files, how to set up your projects, everyday development and git workflows, how to choose models and control cost, and how to extend Claude Code with custom skills, subagents, and MCPs.
 
Whether you're brand new to AI-assisted development or looking to get the most out of Claude Code, this course provides practical, hands-on experience using a real-world application as the running example.
 
This repository contains the source code for the course, which you can use to follow along.

## Getting Started

The **main branch** contains the most up-to-date version of the code, reflecting the latest improvements, updates, and fixes.

Each section in the course has a folder in the repository. The folder contains the source code for the section, containing both a `/start` and `/end` directory—which aligns with the source code at a point in time as it relates to the course.

## Course Structure

- [Section 2: What is Claude Code](./section-02/)
- [Section 3: How Context Works](./section-03/)
- [Section 4: Setting Up Your Projects](./section-04/)
- [Section 5: Development Workflows](./section-05/)
- [Section 7: Git Workflows](./section-07/)
- [Section 8: Customising Claude Code](./section-08/)

## Setup

### Claude Code

Install [Claude Code](https://code.claude.com/docs/en/overview).

[Claude Code documentation](https://code.claude.com/docs/en/)

### .NET
Install [.NET SDK 10](https://dotnet.microsoft.com/download) (LTS)

### Node.js
Install [Node.js 24](https://nodejs.org/) (LTS) — bundles `npm`

### Docker
Install [Docker](https://docs.docker.com/get-docker/) (Desktop or Engine) — required to run PostgreSQL and the integration tests

## The Example Project

The course is built around a small **Expense Tracker** application with a .NET 10 Minimal API backend, a React 19 + Vite frontend, and PostgreSQL for storage.

### Running the application

```shell
docker compose up -d
dotnet ef database update --project api/ExpenseTracker.Api
dotnet run --project api/ExpenseTracker.Api
npm --prefix web install && npm --prefix web run dev
```

Run the last two commands in separate terminals so the API and the web dev server stay up.

| Service  | URL                    | Notes                                              |
|----------|------------------------|----------------------------------------------------|
| API      | http://localhost:5057  | Endpoints: `POST /api/expenses`, `GET /api/expenses` |
| Web      | http://localhost:5173  | Vite dev server; proxies `/api` to the API         |
| Postgres | localhost:5432         | Database `expenses`, user `expenses`               |

### Running tests

```shell
dotnet test
```

The integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a Postgres container, so Docker must be running.

## Resources

### Documentation
- [Claude Code Documentation](https://docs.anthropic.com/en/docs/claude-code)
- [Claude Code GitHub](https://github.com/anthropics/claude-code)
- [Anthropic API Reference](https://docs.anthropic.com/en/api/getting-started)

### Claude Code Settings
- [Settings and Permissions](https://docs.anthropic.com/en/docs/claude-code/settings)
- [CLAUDE.md Reference](https://docs.anthropic.com/en/docs/claude-code/memory)
