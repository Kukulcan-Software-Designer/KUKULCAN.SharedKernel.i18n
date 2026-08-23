# Operations

> **KUKULCAN.SharedKernel.i18n**  
> **Operations, Configuration and Delivery Guide**

---

# 1. Requirements

The service targets:

- .NET 10 SDK/runtime.
- PostgreSQL 14 or newer as the primary relational database.
- Redis 6 or newer when distributed caching is enabled.
- Docker 24 or newer for containerized deployments.

The Infrastructure project uses the PostgreSQL EF Core provider and the API project contains PostgreSQL/Redis health-check integrations.

---

# 2. Configuration

The principal configuration areas are:

| Setting | Purpose |
|---|---|
| `Kukulcan__Database__ConnectionString` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `Jwt__SecretKey` | JWT signing secret; supplied outside source control |
| `Jwt__Issuer` | Expected token issuer |
| `Jwt__Audience` | Expected token audience |
| `Kukulcan__Database__Migration__AutoMigrateOnStartup` | Controls automatic migration at startup |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment |

Do not commit database passwords, JWT signing keys or other secrets. Local/CI integration tests must inject their own disposable configuration.

Example local database configuration:

```bash
export KUKULCAN__DATABASE__CONNECTIONSTRING='Host=localhost;Port=5432;Database=itzamna_i18n;Username=itzamna;Password=change-me'
export CONNECTIONSTRINGS__REDIS='localhost:6379,abortConnect=false'
export JWT__SECRETKEY='LOCAL_ONLY_SECRET_WITH_AT_LEAST_32_CHARACTERS'
```

---

# 3. Local Development

A complete integration setup requires PostgreSQL and, for tests that exercise distributed caching, Redis.

Run the API project with:

```bash
dotnet run --project Source/KUKULCAN.SharedKernel.i18n.API
```

Interactive API documentation is available through Scalar at `/scalar/v1` when enabled by the current environment configuration.

---

# 4. Database Migrations

Create a migration from the actual Infrastructure model:

```bash
dotnet ef migrations add MigrationName \
  --project Source/KUKULCAN.SharedKernel.i18n.Infrastructure/KUKULCAN.SharedKernel.i18n.Infrastructure.csproj \
  --startup-project Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj
```

Apply migrations:

```bash
dotnet ef database update \
  --project Source/KUKULCAN.SharedKernel.i18n.Infrastructure/KUKULCAN.SharedKernel.i18n.Infrastructure.csproj \
  --startup-project Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj
```

The migration and its `ModelSnapshot` are a single schema change and must be reviewed together. Never create a migration manually while omitting or fabricating the corresponding snapshot.

---

# 5. Caching Operations

The cache has two logical levels:

```text
L1: process-local memory
L2: Redis shared cache
Source: PostgreSQL
```

Redis is a performance layer. PostgreSQL remains authoritative. A Redis outage must not corrupt or redefine persisted translation data.

---

# 6. Health and Readiness

The service exposes:

| Endpoint | Meaning |
|---|---|
| `/health` | Overall health |
| `/health/live` | Process liveness |
| `/health/ready` | Dependency readiness |

Readiness is used to verify dependencies required to serve traffic, notably PostgreSQL and Redis when configured as a readiness dependency. Liveness should remain independent of transient external dependency failures.

---

# 7. Logging

The API uses Serilog. Logs should provide enough context to diagnose request and infrastructure failures without recording secrets or sensitive token contents.

---

# 8. Deployment

PostgreSQL is authoritative. Redis provides shared acceleration for repeated reads when enabled.

---

# 9. Testing

Run the complete suite:

```bash
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

The repository contains unit and integration test projects. Integration tests that require infrastructure must run against real PostgreSQL/Redis services supplied by the local environment or CI workflow; they must not silently fall back to an in-memory substitute when the test is intended to verify the real provider.

Coverage can be collected with:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

# 10. Release Checklist

Before releasing a version:

1. Restore and build the complete solution.
2. Run all unit-test projects.
3. Run all applicable integration-test projects against PostgreSQL/Redis.
4. Validate database migrations from the previous release.
5. Validate seed data.
6. Verify translation fallback behavior.
7. Verify default-language protection.
8. Verify JWT policies for read and write endpoints.
9. Verify `/health/live` and `/health/ready`.
10. Verify Scalar/OpenAPI generation.
11. Review the documentation for API or architectural changes.

---

# 11. Operational Invariants

- PostgreSQL remains the source of truth.
- A default active language exists.
- Default-language translation coverage is preserved.
- Cache failures do not corrupt persisted data.
- Read traffic uses `i18n.read` and administrative writes use `i18n.write`.
- Secrets are never committed to the repository.
- Database migrations are applied in a controlled deployment process.

---

# 12. Failure Handling

A missing translation is a functional result, not an infrastructure crash. PostgreSQL connectivity failures are infrastructure failures and must be visible through readiness checks and structured logs. Redis failures should degrade caching rather than invalidate authoritative data.

Authorization failures are handled by the API authentication/authorization pipeline rather than reimplemented inside individual controllers.
