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
| `Kukulcan__Database__Provider` | Database provider; the current service default is `PostgresSql` |
| `Kukulcan__Database__ConnectionString` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `Jwt__SecretKey` | JWT signing secret; supplied outside source control |
| `Jwt__Issuer` | Expected token issuer |
| `Jwt__Audience` | Expected token audience |
| `Kukulcan__Database__Migration__AutoMigrateOnStartup` | Controls automatic migration at startup |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment |
| `ASPNETCORE_HTTP_PORTS` | HTTP port used by the ASP.NET Core application; Docker uses `8080` |

ASP.NET Core environment variables use double underscores (`__`) to represent configuration-section separators (`:`). For example, `Kukulcan__Database__ConnectionString` maps to `Kukulcan:Database:ConnectionString`.

Do not commit database passwords, JWT signing keys or other secrets. Local/CI integration tests must inject their own disposable configuration.

Example local database configuration:

```bash
export KUKULCAN__DATABASE__PROVIDER='PostgresSql'
export KUKULCAN__DATABASE__CONNECTIONSTRING='Host=localhost;Port=5432;Database=itzamna_i18n;Username=itzamna;Password=change-me'
export CONNECTIONSTRINGS__REDIS='localhost:6379,abortConnect=false'
export JWT__SECRETKEY='LOCAL_ONLY_SECRET_WITH_AT_LEAST_32_CHARACTERS'
export JWT__ISSUER='ITZAMNA'
export JWT__AUDIENCE='ITZAMNA.i18n'
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
| `/health` | Overall health; includes all registered health checks |
| `/health/live` | Process liveness; only the `self` check is selected |
| `/health/ready` | Dependency readiness; selects PostgreSQL and Redis checks when they are registered |

The health-check registrations are intentionally separated by tags:

- `self` is tagged `live` and does not depend on PostgreSQL or Redis.
- PostgreSQL is tagged `ready` and `db`.
- Redis is tagged `ready` and `cache`.

Therefore `/health/live` can be used by a container orchestrator for liveness without turning transient PostgreSQL or Redis failures into process-liveness failures. `/health/ready` is the appropriate endpoint for dependency readiness.

---

# 7. Logging

The API uses Serilog. Logs should provide enough context to diagnose request and infrastructure failures without recording secrets or sensitive token contents.

---

# 8. Docker

The repository contains a multi-stage `Dockerfile` at the repository root. The build stage uses the .NET 10 SDK and publishes only the API application; the runtime stage uses the .NET 10 ASP.NET runtime image. The image exposes HTTP port `8080`.

## 8.1 Build the image

Run the build from the repository root so that the Docker build context contains the solution and `Source/` tree:

```bash
docker build --tag kukulcan-sharedkernel-i18n:local .
```

## 8.2 Run the container

A production-like container requires a reachable PostgreSQL instance and, when distributed caching is enabled, a reachable Redis instance. The image does not contain either dependency.

Example:

```bash
docker run --detach \
  --name kukulcan-sharedkernel-i18n \
  --publish 8080:8080 \
  --env ASPNETCORE_HTTP_PORTS=8080 \
  --env KUKULCAN__DATABASE__PROVIDER=PostgresSql \
  --env KUKULCAN__DATABASE__CONNECTIONSTRING='Host=<postgres-host>;Port=5432;Database=<database>;Username=<username>;Password=<password>' \
  --env CONNECTIONSTRINGS__REDIS='<redis-host>:6379,abortConnect=false' \
  --env JWT__SECRETKEY='<secret-at-least-32-characters>' \
  --env JWT__ISSUER='ITZAMNA' \
  --env JWT__AUDIENCE='ITZAMNA.i18n' \
  kukulcan-sharedkernel-i18n:local
```

The PostgreSQL and Redis host values must be resolvable and reachable from inside the container. Do not use `localhost` for an external dependency unless that dependency is running inside the same container.

## 8.3 Database configuration

`Kukulcan__Database__Provider` selects the database provider. The current application configuration defaults this value to `PostgresSql`.

`Kukulcan__Database__ConnectionString` is required by the KUKULCAN database infrastructure during application startup. For a real deployment it must contain the connection string for the authoritative PostgreSQL database.

Database migrations are not automatically applied merely because the application is running in Docker. Automatic migration is controlled separately by `Kukulcan__Database__Migration__AutoMigrateOnStartup` and should only be enabled when that deployment policy is explicitly intended.

## 8.4 JWT configuration

The API requires `Jwt__SecretKey` at startup and rejects keys shorter than 32 characters. `Jwt__Issuer` and `Jwt__Audience` define the expected token issuer and audience; the documented defaults are `ITZAMNA` and `ITZAMNA.i18n`.

The signing secret must be supplied through deployment configuration or a secret-management mechanism and must never be committed to the repository.

## 8.5 Redis configuration

Redis is optional at the infrastructure-registration level: when `ConnectionStrings__Redis` is empty, the application uses its memory-only cache implementation. When a Redis connection string is configured, the distributed cache implementation is registered and the Redis health check is added.

The container should receive the actual Redis endpoint through `ConnectionStrings__Redis` rather than relying on `localhost`.

## 8.6 Docker health checks

The container exposes the application HTTP endpoint on port `8080`. The CI smoke test publishes that port and polls:

```text
http://127.0.0.1:8080/health/live
```

The smoke test intentionally validates liveness only. It does not require a PostgreSQL or Redis service to answer the liveness request.

The CI container receives:

```text
ASPNETCORE_HTTP_PORTS=8080
KUKULCAN__DATABASE__CONNECTIONSTRING=<non-empty smoke-test placeholder>
Jwt__SecretKey=<non-production smoke-test secret>
```

The smoke-test database value exists to satisfy the application's required startup configuration; it is not a production database configuration. The test does not use the real local `Atlas` database connection string and does not start an ephemeral PostgreSQL container for the Docker smoke test.

---

# 9. Deployment

A real deployment requires:

1. A reachable PostgreSQL 14+ instance containing the i18n schema and data.
2. A reachable Redis 6+ instance when distributed caching is enabled.
3. A securely managed JWT signing secret of at least 32 characters.
4. Network/DNS configuration allowing the API container to reach its dependencies.
5. Database migration handling appropriate to the deployment process.

PostgreSQL is authoritative. Redis provides shared acceleration for repeated reads when enabled.

---

# 10. Testing

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

# 11. Release Checklist

Before releasing a version:

1. Restore and build the complete solution.
2. Run all unit-test projects.
3. Run all applicable integration-test projects against PostgreSQL/Redis.
4. Build the Docker image from the repository root.
5. Start the container with deployment-specific configuration and verify `/health/live`.
6. Verify `/health/ready` against the actual PostgreSQL/Redis dependencies.
7. Validate database migrations from the previous release.
8. Validate seed data.
9. Verify translation fallback behavior.
10. Verify default-language protection.
11. Verify JWT policies for read and write endpoints.
12. Verify Scalar/OpenAPI generation.
13. Review the documentation for API, deployment or architectural changes.

---

# 12. Operational Invariants

- PostgreSQL remains the source of truth.
- A default active language exists.
- Default-language translation coverage is preserved.
- Cache failures do not corrupt persisted data.
- Read traffic uses `i18n.read` and administrative writes use `i18n.write`.
- Secrets are never committed to the repository.
- Database migrations are applied in a controlled deployment process.
- Docker liveness is independent of PostgreSQL and Redis availability.

---

# 13. Failure Handling

A missing translation is a functional result, not an infrastructure crash. PostgreSQL connectivity failures are infrastructure failures and must be visible through readiness checks and structured logs. Redis failures should degrade caching rather than invalidate authoritative data.

Authorization failures are handled by the API authentication/authorization pipeline rather than reimplemented inside individual controllers.
