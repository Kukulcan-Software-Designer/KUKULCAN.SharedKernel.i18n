# Governance

## Purpose

The purpose of this document is to define the long-term governance model of **KUKULCAN.SharedKernel.i18n**.

Unlike many libraries that evolve organically over time, KUKULCAN.SharedKernel.i18n follows a deliberate architectural process designed to maximize:

- Stability
- Predictability
- Maintainability
- Backward compatibility
- Architectural consistency

The architecture is considered a first-class artifact of the project.

---

# Vision

KUKULCAN.SharedKernel.i18n aims to become a stable architectural foundation for enterprise software built on top of:

- Domain-Driven Design (DDD)
- Clean Architecture
- CQRS
- Event-Driven Design

The framework intentionally focuses on quality rather than quantity.

New functionality will only be introduced when it clearly improves the architecture.

---

# Core Principles

Project decisions are guided by the following principles.

## Simplicity

Simple solutions are preferred over clever solutions.

---

## Explicitness

The framework favors explicit APIs over implicit behavior.

---

## Stability

Public APIs should remain stable for as long as possible.

Breaking changes are exceptional.

---

## Backward Compatibility

Backward compatibility is considered a strategic objective.

Whenever possible, existing APIs will continue working across future releases.

---

## Low Coupling

Modules should remain independent.

Dependencies between modules should always follow a clear direction.

---

## High Cohesion

Every module should solve exactly one problem.

No module should become a "miscellaneous utilities" container.

---

## Framework Independence

The Shared Kernel should not depend on:

- ASP.NET Core
- Entity Framework
- Logging frameworks
- HTTP
- Serialization frameworks

The framework should remain infrastructure-independent.

---

# Architectural Lifecycle

Every module follows the same lifecycle.

```
Design
        ↓
Implementation
        ↓
Audit
        ↓
Refactoring
        ↓
Freeze
        ↓
Maintenance
```

Once frozen, modules are considered stable.

Future modifications should preserve compatibility whenever possible.

---

# Frozen Modules

The following modules are considered architecturally frozen.

- Abstractions
- Attributes
- Collections
- Domain
- DomainEvents
- Exceptions
- Globalization
- Guards
- Identifiers
- Internals
- Maybe
- Results
- Specifications
- Time
- Validation
- Versioning

Frozen modules should not receive breaking changes during the current major version.

---

# Decision Process

Architectural decisions should follow the following order.

## Step 1

Identify the problem.

---

## Step 2

Evaluate existing APIs.

---

## Step 3

Determine whether the problem can be solved without changing the public API.

---

## Step 4

If a public API change is unavoidable:

- evaluate compatibility;
- evaluate long-term impact;
- evaluate architectural consequences.

---

## Step 5

Only then should implementation begin.

---

# Architectural Reviews

Every significant change should answer the following questions.

## Responsibility

Does the new code belong to this module?

---

## Cohesion

Does the module still solve one problem?

---

## Coupling

Does the change introduce unnecessary dependencies?

---

## API

Does the public API remain simple?

---

## Naming

Is the terminology consistent with the rest of the framework?

---

## Testing

Can the feature be tested independently?

---

# Public API Policy

Every public type becomes part of the framework contract.

New public APIs should therefore be introduced conservatively.

Whenever possible:

Prefer

```csharp
internal
```

instead of

```csharp
public
```

---

# Breaking Changes

Breaking changes are only acceptable when:

- the existing API is fundamentally incorrect;
- the change significantly improves the architecture;
- no backward-compatible alternative exists.

Breaking changes should normally occur only in future major versions.

---

# Module Ownership

Every module should have a clearly defined architectural owner.

Contributors are encouraged to discuss significant architectural proposals before implementation.

---

# Documentation Policy

Documentation is considered part of the source code.

Every public API should include:

- XML documentation;
- examples when appropriate;
- architectural rationale when useful.

README, CHANGELOG and documentation should evolve together with the framework.

---

# Release Policy

Releases follow Semantic Versioning 2.0.

```
Major.Minor.Patch
```

Examples

```
1.0.0

1.1.0

1.1.3

2.0.0
```

Pre-release versions:

```
1.0.0-alpha1

1.0.0-beta1

1.0.0-rc1
```

---

# Quality Gates

Before any release the following conditions should be met.

- Successful build.
- Successful unit tests.
- No compiler warnings.
- No nullable warnings.
- XML documentation completed.
- Public API reviewed.
- Architectural audit completed.
- CHANGELOG updated.
- Documentation updated.

---

# Long-Term Maintenance

The project prioritizes long-term stability over rapid feature growth.

Future releases will primarily focus on:

- Bug fixes.
- Performance improvements.
- Documentation.
- Better developer experience.
- Additional tests.

Large architectural redesigns are intentionally uncommon.

---

# Community Governance

Architectural discussions are encouraged.

Technical disagreement is welcome.

Architectural consistency always has priority over personal preference.

Every contributor is expected to:

- respect the project's architectural principles;
- preserve API quality;
- avoid unnecessary complexity;
- think about long-term maintainability.

---

# Final Statement

KUKULCAN.SharedKernel.i18n is not intended to become a collection of utilities.

It is intended to become a stable architectural foundation for enterprise software.

Every architectural decision should therefore be evaluated according to one simple question:

> **"Will this make the framework easier to maintain ten years from now?"**

If the answer is **yes**, the change is probably worth considering.

If the answer is **no**, the change probably does not belong in the Shared Kernel.
