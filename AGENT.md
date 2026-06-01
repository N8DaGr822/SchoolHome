# AGENT.md

## Instructions

This is a C#/.NET application. Keep changes small, safe, and consistent with the existing codebase.

## Rules

* Follow existing patterns before introducing new ones.
* Do not hardcode secrets, tokens, connection strings, tenant IDs, or environment-specific URLs.
* Preserve authentication, authorization, validation, logging, and error handling.
* Keep controllers/components thin; put business logic in services.
* Use async/await for I/O work and pass `CancellationToken` where supported.
* Avoid broad refactors unless explicitly requested.
* Do not add packages unless necessary.
* Check related call sites when changing shared models, DTOs, services, or clients.

## Validation

Before finishing, run the relevant checks when possible:

```bash
dotnet restore
dotnet build
dotnet test
```

Summarize what changed, why it changed, and any follow-up configuration needed.
