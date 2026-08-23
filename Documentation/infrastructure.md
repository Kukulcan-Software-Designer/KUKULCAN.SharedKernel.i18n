# Infrastructure

> **KUKULCAN.SharedKernel.i18n**  
> **Infrastructure and Persistence Reference**

---

# 1. Purpose

The Infrastructure project contains the technical implementations required to run the i18n service. It owns the concrete database provider and caching implementations.

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
│   └── I18NDbContext.cs
├── Primitives/
├── Services/
└── InfrastructureServiceRegistration.cs
```

---

# 3. Database Context

`I18NDbContext` is the module-specific EF Core context. It exposes the persistence model for languages, translations, locale configurations and currency formats and applies the entity configurations from `Persistence/Configurations`.

The context is infrastructure-owned. Domain entities do not inherit from EF Core types and do not contain provider-specific mapping configuration.

The context is registered through the infrastructure composition root and receives `KukulcanDatabaseOptions` from configuration. Design-time EF Core operations therefore require a valid `Kukulcan__Database__ConnectionString` configuration value when the startup project is used.

---

# 4. Entity Configurations

EF Core configuration is separated from the domain model using `IEntityTypeConfiguration<T>` implementations. This keeps table names, column sizes, keys, indexes, unique constraints, relationships and value-object conversions outside domain entities.

The database schema should enforce the same fundamental uniqueness guarantees that the domain assumes, particularly around translation code/language combinations and the single default-language invariant.

---

# 5. PostgreSQL

PostgreSQL is the default database provider for this module. The provider package is referenced by Infrastructure, not by Domain or Application.

```text
Domain          -> no PostgreSQL knowledge
Application     -> no PostgreSQL knowledge
Infrastructure  -> Npgsql / EF Core
```

---

# 6. Repositories

Repository implementations live under `Persistence/Repositories` and implement contracts declared by the inner layer. Repositories perform persistence-oriented operations and must not become generic business services.

---

# 7. Seed Data

Seed configuration lives under `Persistence/Seeds`. Seed changes must be reviewed as data-contract changes because consuming applications may rely on core language records.

---

# 8. Caching

The service uses process-local memory caching and an optional Redis-backed distributed cache. PostgreSQL remains the authoritative persistence store.

Redis is an optimization layer. Its failure may reduce cache efficiency but must not change persisted data correctness.

---

# 9. Dependency Injection

`InfrastructureServiceRegistration` is the infrastructure composition entry point. It registers the `I18NDbContext`, repositories, cache services, PostgreSQL provider configuration and Redis/memory-cache implementations.

---

# 10. Relationship With SharedKernel.Database

`KUKULCAN.SharedKernel.Database` provides common persistence concerns such as EF Core base infrastructure, unit-of-work support, audit behavior and other cross-cutting persistence capabilities. The i18n module reuses those capabilities rather than duplicating them.

---

# 11. Migrations

EF Core migrations belong to the Infrastructure project. A migration consists of the migration files **and the corresponding `ModelSnapshot`**. Both must be generated from the same real model state.

Typical commands:

```bash
dotnet ef migrations add MigrationName \
  --project Source/KUKULCAN.SharedKernel.i18n.Infrastructure/KUKULCAN.SharedKernel.i18n.Infrastructure.csproj \
  --startup-project Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj

dotnet ef database update \
  --project Source/KUKULCAN.SharedKernel.i18n.Infrastructure/KUKULCAN.SharedKernel.i18n.Infrastructure.csproj \
  --startup-project Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj
```

Review migrations for accidental schema changes before deployment and keep the snapshot synchronized with the model.

---

# 12. Infrastructure Design Rules

1. Do not move business invariants into EF configurations.
2. Do not expose EF Core types through domain contracts.
3. Keep provider-specific packages in Infrastructure.
4. Keep cache mechanics out of controllers.
5. Treat PostgreSQL as the authoritative store.
6. Treat Redis as a performance layer, not a source of truth.
7. Reuse `KUKULCAN.SharedKernel.Database` instead of implementing duplicate persistence infrastructure.
