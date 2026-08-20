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

The Infrastructure project currently uses the PostgreSQL EF Core provider and the API project contains PostgreSQL/Redis health-check integrations.

---

# 2. Configuration

The principal configuration areas are:

| Setting | Purpose |
|---|---|
| `ConnectionStrings__I18nDb` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string; empty disables distributed cache |
| `Jwt__SecretKey` | JWT signing secret |
| `Jwt__Issuer` | Expected token issuer |
| `Jwt__Audience` | Expected token audience |
| `Database__AutoMigrate` | Controls startup migration behavior where enabled |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment |

Secrets must be supplied through a secure configuration mechanism in production rather than committed to source control.

---

# 3. Local Development

A minimal local setup requires PostgreSQL. Redis is optional because the service contains a memory-cache fallback.

Example environment variables:

```bash
export ConnectionStrings__I18nDb="Host=localhost;Port=5432;Database=kukulcan_i18n;Username=kukulcan;Password=change-me"
export ConnectionStrings__Redis=""
export Jwt__SecretKey="LOCAL_ONLY_SECRET_WITH_AT_LEAST_32_CHARACTERS"
```

Run the API project with:

```bash
dotnet run --project Source/KUKULCAN.SharedKernel.i18n.API
```

Interactive API documentation is available through Scalar at the host's `/scalar/v1` route when enabled by the current environment configuration.

---

# 4. Database Migrations

Create a migration:

```bash
dotnet ef migrations add MigrationName \
  --project Source/KUKULCAN.SharedKernel.i18n.Infrastructure \
  --startup-project Source/KUKULCAN.SharedKernel.i18n.API
```

Apply migrations:

```bash
dotnet ef database update \
  --project Source/KUKULCAN.SharedKernel.i18n.Infrastructure \
  --startup-project Source/KUKULCAN.SharedKernel.i18n.API
```

Schema changes must be tested against a clean database and an upgraded database. Seed behavior should also be verified after migrations.

---

# 5. Caching Operations

The cache has two logical levels:

```text
L1: process-local memory
L2: Redis shared cache
Source: PostgreSQL
```

Recommended service lifetimes from the current contract:

| Data | TTL |
|---|---:|
| Translation lookup | 1 hour |
| Locale configuration | 6 hours |
| Currency formats | 6 hours |

When Redis is unavailable or deliberately not configured, the memory-only implementation can be used. This is suitable for development and single-instance deployments; multi-instance deployments benefit from Redis so that cache state is shared.

---

# 6. Health and Readiness

The service exposes three operational probes:

| Endpoint | Meaning |
|---|---|
| `/health` | Overall health |
| `/health/live` | Process liveness |
| `/health/ready` | Dependency readiness |

Readiness should be used by orchestrators to decide whether traffic can be routed to the instance. Liveness should not depend on external databases because a transient database outage should not cause the process to be restarted unnecessarily.

---

# 7. Logging

The API uses Serilog with console/file sinks and environment enrichment.

Logs should include enough context to diagnose request and infrastructure failures without recording secrets or sensitive token contents.

Administrative bulk operations should be observable through normal request logging and application behavior logging.

---

# 8. Deployment

The recommended production topology is:

```text
                ┌───────────────┐
Clients ───────>│ Load Balancer │
                └───────┬───────┘
                        |
              ┌─────────┴─────────┐
              v                   v
        i18n API instance   i18n API instance
              |                   |
              └─────────┬─────────┘
                        |
             ┌──────────┴──────────┐
             v                     v
        PostgreSQL              Redis
```

PostgreSQL is authoritative. Redis is shared acceleration for repeated reads.

---

# 9. Testing

Run the complete test suite:

```bash
dotnet test
```

Run individual architectural layers:

```bash
dotnet test Tests/KUKULCAN.SharedKernel.i18n.Domain.UnitTests
dotnet test Tests/KUKULCAN.SharedKernel.i18n.Application.UnitTests
dotnet test Tests/KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests
dotnet test Tests/KUKULCAN.SharedKernel.i18n.API.UnitTests
```

Coverage can be collected with:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

The CI workflow is split by architectural layer so that failures can be located quickly.

---

# 10. Release Checklist

Before releasing a version:

1. Restore and build the complete solution.
2. Run all unit-test projects.
3. Validate database migrations from the previous release.
4. Validate seed data.
5. Verify translation fallback behavior.
6. Verify default-language protection.
7. Verify JWT policies for read and write endpoints.
8. Verify `/health/live` and `/health/ready`.
9. Verify Scalar/OpenAPI generation.
10. Review the documentation for API or architectural changes.

---

# 11. Operational Invariants

The following properties must remain true in production:

- PostgreSQL remains the source of truth.
- A default active language exists.
- Default-language translation coverage is preserved.
- Cache failures do not corrupt persisted data.
- Read traffic uses `i18n.read` and administrative writes use `i18n.write`.
- Secrets are never committed to the repository.
- Database migrations are applied in a controlled deployment process.

---

# 12. Failure Handling

A translation lookup failure caused by a missing translation is a functional result and should not be treated as an infrastructure crash.

A PostgreSQL connectivity failure is an infrastructure failure and should be visible through readiness checks and structured logs.

A Redis failure should degrade caching rather than invalidate the service's authoritative data model.

Authorization failures should be returned by the API security middleware/policy pipeline rather than reimplemented inside individual controllers.
