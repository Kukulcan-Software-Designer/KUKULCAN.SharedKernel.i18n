# Documentation

> **KUKULCAN.SharedKernel.i18n**
>
> **Internationalization Service — Documentation Handbook**
>
> Status: **Stable**  
> Target: **.NET 10**

---

## Purpose

This directory is the authoritative technical documentation for `KUKULCAN.SharedKernel.i18n`.

The documentation follows the same separation used by `KUKULCAN.SharedKernel`: architecture explains structural decisions, while module documents explain responsibilities, contracts and usage. It also follows the infrastructure boundary established by `KUKULCAN.SharedKernel.Database`.

The repository is organized as a Clean Architecture solution with four production projects and four dedicated test projects.

## Documentation Map

| Document | Scope |
|---|---|
| [architecture.md](architecture.md) | System architecture, dependency rules and design decisions |
| [domain.md](domain.md) | Domain model, entities, value objects, services and contracts |
| [application.md](application.md) | CQRS, MediatR features, validation, pagination and application contracts |
| [infrastructure.md](infrastructure.md) | EF Core persistence, repositories, PostgreSQL, cache and registration |
| [api.md](api.md) | HTTP API, endpoints, authorization, responses and operational behavior |
| [operations.md](operations.md) | Configuration, deployment, health checks, caching, migrations and testing |

## Solution Structure

```text
KUKULCAN.SharedKernel.i18n/
│
├── Documentation/
│   ├── README.md
│   ├── architecture.md
│   ├── domain.md
│   ├── application.md
│   ├── infrastructure.md
│   ├── api.md
│   └── operations.md
│
├── Source/
│   ├── KUKULCAN.SharedKernel.i18n.Domain/
│   ├── KUKULCAN.SharedKernel.i18n.Application/
│   ├── KUKULCAN.SharedKernel.i18n.Infrastructure/
│   └── KUKULCAN.SharedKernel.i18n.API/
│
├── SourceClient/
│   └── KUKULCAN.SharedKernel.i18n.Client/
│
└── Tests/
    ├── KUKULCAN.SharedKernel.i18n.Domain.UnitTests/
    ├── KUKULCAN.SharedKernel.i18n.Application.UnitTests/
    ├── KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests/
    └── KUKULCAN.SharedKernel.i18n.API.UnitTests/
```

## Documentation Principles

- Documentation describes the implementation that exists in the repository, not an idealized future architecture.
- Public API behavior is documented from the controller, application and domain contracts.
- Infrastructure details remain separate from domain rules.
- Examples use the repository's actual route and naming conventions.
- Architectural constraints are treated as part of the project's contract.

## Relationship With the Shared Kernel

`KUKULCAN.SharedKernel.i18n` is a bounded-context service built on top of `KUKULCAN.SharedKernel`. It consumes shared primitives such as entities, value objects, results and domain-event infrastructure rather than redefining those concepts.

Persistence is delegated to `KUKULCAN.SharedKernel.Database`; PostgreSQL and Redis-specific packages remain in the i18n infrastructure project.

## Documentation Status

The handbook is intended to evolve with the source code. When a public endpoint, domain rule, dependency boundary or persistence strategy changes, the corresponding document must be reviewed in the same change.
