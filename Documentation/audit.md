# KUKULCAN.SharedKernel.i18n — Final Audit

## Scope

Audit of `KUKULCAN.SharedKernel.i18n` on branch `main` covering testing, HTTP error handling, security, EF Core persistence, CI/CD and documentation.

## A. Coverage

- Domain unit tests are present.
- Application unit and integration tests are present.
- Infrastructure unit and integration tests are present.
- API unit tests cover controller metadata/behaviour and middleware behaviour.
- API integration tests exercise PostgreSQL and Redis through Testcontainers.
- API integration coverage includes Languages, Currencies, Locales and Translations.
- Integration tests now exercise the real EF Core migrations instead of relying on `EnsureCreated`.

## B. HTTP errors and validation

- Expected application failures are represented by `Result`/`Error` and mapped to HTTP responses by `ResultExtensions`.
- Validation exceptions are converted to RFC 7807-style `application/problem+json` responses with HTTP 422.
- Unexpected exceptions return a generic HTTP 500 response without exposing the exception detail.
- Controller authorization metadata is covered by API unit tests.

## C. Security

- JWT bearer authentication validates issuer, audience, lifetime and signing key.
- Read operations require authentication through `i18n.read`.
- Write operations require `KUKULCAN.Admin` or `KUKULCAN.i18n.Admin`.
- The API no longer supplies a fallback JWT signing key in source code.
- `Jwt:SecretKey` is required and must contain at least 32 characters.
- Production deployments must provide the signing key through deployment configuration/secrets rather than source control.

## D. EF Core

- PostgreSQL uses `Kukulcan:Database:ConnectionString`.
- The startup migration switch is `Kukulcan:Database:Migration:AutoMigrateOnStartup`.
- The initial EF Core migration is committed under `Source/KUKULCAN.SharedKernel.i18n.Infrastructure/Migrations`.
- Infrastructure integration tests verify that pending migrations are applied and that the migration history contains `20260823095510_InitialCreation`.

## E. CI/CD

The repository contains independent GitHub Actions workflows for domain, application, infrastructure and API unit/integration tests. The integration workflows use real PostgreSQL/Redis services through the existing Testcontainers-based tests.

## F. Documentation

This audit records the final architectural and operational decisions. Existing API, architecture, application, domain, infrastructure and operations documentation remains part of the repository documentation set.

## G. Freeze

The module is considered frozen after the audit when all required GitHub Actions workflows are green on `main`.

Changes after the freeze require a concrete defect, security issue, compatibility issue or explicitly approved new requirement.
