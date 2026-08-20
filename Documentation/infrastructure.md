# Infrastructure

> **KUKULCAN.SharedKernel.i18n**  
> **Infrastructure and Persistence Reference**

---

# 1. Purpose

The Infrastructure project contains the technical implementations required to run the i18n service. It is the only production project that owns the database provider and concrete caching implementations.

The project consumes `KUKULCAN.SharedKernel.Database` as its shared EF Core persistence foundation.

---

# 2. Main Components

```text
Infrastructure/
├── Abstractions/
├── Persistence/
│   ├── Configurations/
│   ├── Repositories/
│   ├── Seeds/
│   └── I18nDbContext.cs
├── Primitives/
├── Services/
└── InfrastructureServiceRegistration.cs
```

---

# 3. Database Context

`I18nDbContext` is the module-specific EF Core context. It exposes the persistence model for languages, translations, locale configurations and currency formats and applies the entity configurations from the `Persistence/Configurations` directory.

The context is infrastructure-owned. Domain entities do not inherit from EF Core types and do not contain provider-specific mapping configuration.

---

# 4. Entity Configurations

EF Core configuration is separated from the domain model using `IEntityTypeConfiguration<T>` implementations.

This keeps concerns such as:

- Table names.
- Column sizes.
- Keys and indexes.
- Unique constraints.
- Relationships.
- Value-object conversion.

outside the domain entities.

The database schema should enforce the same fundamental uniqueness guarantees that the domain assumes, particularly around translation code/language combinations.

---

# 5. PostgreSQL

PostgreSQL is the default database provider for this module. The provider package is referenced only by Infrastructure, not by Domain or Application.

This preserves the architectural boundary:

```text
Domain       -> no PostgreSQL knowledge
Application  -> no PostgreSQL knowledge
Infrastructure -> Npgsql / EF Core
```

The module therefore remains independent of provider-specific SQL in its business model.

---

# 6. Repositories

Repository implementations live under `Persistence/Repositories` and implement contracts declared by the inner layer.

Repositories are responsible for persistence-oriented operations such as:

- Loading a language.
- Loading a translation by code/language.
- Loading all translations for a module.
- Loading variants.
- Reading/writing locale configuration.
- Reading/writing currency formats.

Repositories should not become generic business services. Fallback rules and state-transition rules belong to domain/application services.

---

# 7. Seed Data

Seed configuration lives under `Persistence/Seeds`.

The seed process establishes the initial platform languages and their locale/currency configuration. Translation seed data should preserve the default-language requirement so that fallback has a valid base.

Seed changes must be reviewed as data-contract changes because consuming applications may rely on the availability of core language records.

---

# 8. Caching

The service uses a two-level cache strategy.

| Level | Implementation | Purpose |
|---|---|---|
| L1 | `MemoryOnlyCacheService` / memory cache | Fast local reads per application instance |
| L2 | Redis-backed distributed cache | Shared cache across service replicas |
| Source | PostgreSQL | Authoritative persistence |

Recommended default lifetimes in the current service contract are:

- Translations: 1 hour.
- Locale configuration: 6 hours.
- Currency configuration: 6 hours.

The exact implementation should remain centralized in the infrastructure cache services rather than in controllers or repositories.

---

# 9. Cache Failure Strategy

Redis is an optimization, not the source of truth. If no Redis connection is configured, the service can fall back to memory-only caching.

A Redis outage must therefore not change the correctness of translation data. It may reduce cache efficiency, but PostgreSQL remains authoritative.

Cache invalidation is coupled to successful write operations. Stale data should never be intentionally served after a successful administrative update when the infrastructure can invalidate the relevant key.

---

# 10. Dependency Injection

`InfrastructureServiceRegistration` is the infrastructure composition entry point.

It registers:

- `I18nDbContext`.
- Domain repository implementations.
- Cache services.
- PostgreSQL provider configuration.
- Redis or memory-cache implementation.
- Supporting logging/configuration services.

The host should call the registration methods rather than constructing infrastructure objects manually.

---

# 11. Relationship With SharedKernel.Database

`KUKULCAN.SharedKernel.Database` provides common persistence concerns such as EF Core base infrastructure, unit-of-work support, audit behavior and other cross-cutting persistence capabilities.

The i18n module should use those capabilities rather than duplicating them.

The boundary is:

```text
KUKULCAN.SharedKernel
        ^
        |
KUKULCAN.SharedKernel.Database
        ^
        |
KUKULCAN.SharedKernel.i18n.Infrastructure
```

The module-specific infrastructure remains responsible for its own entity mappings, repositories, provider configuration, seeds and cache implementation.

---

# 12. Migrations

EF Core migrations belong to the Infrastructure project because the database schema is an infrastructure artifact.

Typical commands are:

```bash
dotnet ef migrations add MigrationName \
  --project Source/KUKULCAN.SharedKernel.i18n.Infrastructure \
  --startup-project Source/KUKULCAN.SharedKernel.i18n.API

dotnet ef database update \
  --project Source/KUKULCAN.SharedKernel.i18n.Infrastructure \
  --startup-project Source/KUKULCAN.SharedKernel.i18n.API
```

Migrations should be reviewed for accidental schema changes before deployment.

---

# 13. Infrastructure Design Rules

1. Do not move business invariants into EF configurations.
2. Do not expose EF Core types through domain contracts.
3. Keep provider-specific packages in Infrastructure.
4. Keep cache mechanics out of controllers.
5. Treat PostgreSQL as the authoritative store.
6. Treat Redis as a performance layer, not a source of truth.
7. Reuse `KUKULCAN.SharedKernel.Database` instead of implementing duplicate persistence infrastructure.
