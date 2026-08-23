# Documentation Audit

> **KUKULCAN.SharedKernel.i18n**  
> Documentation audit baseline for `documentation/Auditoria`

## 1. Scope

This audit compares the documentation currently present in the repository with the implementation and configuration on the current `main` baseline. No production-code changes are part of this first pass.

Audited documentation:

- `README.md`
- `Documentation/README.md`
- `Documentation/architecture.md`
- `Documentation/domain.md`
- `Documentation/application.md`
- `Documentation/infrastructure.md`
- `Documentation/api.md`
- `Documentation/operations.md`
- `API_GUIDELINES.md`
- `CHANGELOG.md`

Implementation/configuration cross-checks started with:

- `Source/KUKULCAN.SharedKernel.i18n.Infrastructure/KUKULCAN.SharedKernel.i18n.Infrastructure.csproj`
- `Source/KUKULCAN.SharedKernel.i18n.API/appsettings.json`
- current solution/workflow structure

## 2. Initial Findings

### D-001 — Root README contains obsolete project paths

**Severity:** Medium  
**Status:** Open

The root `README.md` still contains paths such as `src/...` and `tests/...`, while the repository currently uses `Source/...`, `SourceClient/...` and `Tests/...`.

The documented EF CLI commands also use the obsolete `src/...` layout. These commands must be updated to the current repository paths.

### D-002 — Root README database requirements are inconsistent with the current implementation

**Severity:** Medium  
**Status:** Open

The root README lists `SQL Server / PostgreSQL 14+` as a requirement. The current Infrastructure project explicitly references `Npgsql.EntityFrameworkCore.PostgreSQL` and `KUKULCAN.SharedKernel.Database`; the current API configuration declares `PostgresSql` as the provider.

Documentation should identify PostgreSQL as the supported relational provider for this service unless another provider is demonstrably supported by the current code.

### D-003 — Root README configuration names do not match the current database configuration contract

**Severity:** High  
**Status:** Open

The root README documents `ConnectionStrings__I18nDb` as the database connection setting. The current API configuration contains the database connection under:

```text
Kukulcan:Database:ConnectionString
```

Therefore the documented environment-variable form should be:

```text
Kukulcan__Database__ConnectionString
```

The Redis setting remains under `ConnectionStrings:Redis`.

### D-004 — Root README migration instructions are stale

**Severity:** High  
**Status:** Open

The migration examples use obsolete `src/...` paths. They also need to reflect the current infrastructure/API project paths and the repository's current migration workflow.

The documentation should explicitly state that migrations are generated from the actual EF model and must include the corresponding `ModelSnapshot`.

### D-005 — Test project names in root README are obsolete

**Severity:** Medium  
**Status:** Open

The root README refers to test directories such as `tests/KUKULCAN.SharedKernel.i18n.Domain.Tests`, while the repository currently uses `Tests/KUKULCAN.SharedKernel.i18n.Domain.UnitTests`, `Application.UnitTests`, `Infrastructure.UnitTests`, `API.UnitTests`, plus dedicated integration projects.

The complete test topology should be documented.

### D-006 — Documentation/README test structure is incomplete

**Severity:** Medium  
**Status:** Open

`Documentation/README.md` currently describes four dedicated test projects, but the current solution also contains integration test projects. The documentation map should distinguish unit and integration test projects and describe their external dependencies.

### D-007 — Operations documentation contains a stale statement about CI workflow structure

**Severity:** Medium  
**Status:** Open

`Documentation/operations.md` states that the CI workflow is split by architectural layer. The current repository now also contains a global CI workflow that restores/builds the solution and executes all unit and integration test projects.

The documentation must describe both the existing specific workflows and the global CI workflow.

### D-008 — JWT configuration needs one authoritative documented contract

**Severity:** Medium  
**Status:** Open

The repository has JWT configuration for issuer/audience and a secret supplied outside source control. The documentation should use one canonical configuration section and explicitly distinguish non-secret settings from the secret. The secret must never be represented by a real value in committed documentation.

### D-009 — Health-check documentation must reflect actual readiness dependencies

**Severity:** Medium  
**Status:** Open

The documentation states that readiness checks PostgreSQL and Redis. This must be cross-checked against the current health-check registration and integration tests before being retained as an invariant.

### D-010 — API authorization documentation requires final implementation cross-check

**Severity:** Medium  
**Status:** Open

`README.md` and `Documentation/operations.md` describe `i18n.read` and `i18n.write` policies. The final documentation must be checked against the actual controller attributes/policies and `AddKukulcanI18NApi` registration so that documented authorization never diverges from the implementation.

## 3. Documentation That Appears Structurally Complete

The following documentation areas already exist and should be refined rather than replaced:

- Architecture
- Domain
- Application
- Infrastructure
- API
- Operations
- Repository contribution/governance documents
- API guidelines
- Changelog

## 4. Correction Strategy

Corrections will be minimal and documentation-only unless a documented behavior is proven to be wrong in the implementation.

Priority order:

1. Correct obsolete paths and commands.
2. Correct configuration names and EF Core migration instructions.
3. Synchronize test and CI/CD documentation.
4. Cross-check API authorization and health checks against source.
5. Review architecture/domain/application/infrastructure/API documents for implementation drift.
6. Normalize links and terminology.
7. Run the complete CI pipeline after documentation changes.

## 5. Exit Criteria for Phase F

Phase F is complete when:

- Documentation describes the current repository layout.
- Configuration examples match the current configuration contract.
- No secret is committed or exposed in examples.
- EF Core migration instructions are correct.
- Unit and integration tests are documented accurately.
- PostgreSQL/Redis integration behavior is documented accurately.
- CI/CD behavior is documented accurately.
- API endpoints, authorization and health checks match implementation.
- All documentation changes are validated by the complete CI pipeline.
