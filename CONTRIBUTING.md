# Contributing to KUKULCAN.SharedKernel.i18n

First of all, thank you for your interest in contributing to **KUKULCAN.SharedKernel.i18n**.

This project aims to provide a long-term, stable and maintainable architectural foundation for enterprise applications following **Domain-Driven Design (DDD)** and **Clean Architecture**.

Every contribution, regardless of its size, is greatly appreciated.

---

# Philosophy

The primary objective of this project is **architectural stability**.

New features are always welcome, but never at the expense of:

- API consistency
- Architectural cohesion
- Backward compatibility
- Simplicity
- Long-term maintainability

Whenever there is a trade-off between adding functionality and preserving architectural quality, architectural quality takes precedence.

---

# Before Contributing

Before opening an Issue or a Pull Request, please:

- Read the README.
- Read the architecture documentation.
- Search existing Issues.
- Search existing Pull Requests.
- Verify that the feature has not already been proposed.

---

# Ways to Contribute

Contributions may include:

- Bug reports
- Bug fixes
- Documentation improvements
- Unit tests
- Performance improvements
- Architectural improvements
- New functionality
- Refactoring

---

# What We Expect

Every contribution should:

- Respect the existing architecture.
- Preserve backward compatibility whenever possible.
- Follow the existing coding conventions.
- Include XML documentation.
- Include unit tests whenever applicable.
- Keep the public API minimal.

---

# Pull Request Workflow

The recommended workflow is:

1. Fork the repository.
2. Create a feature branch.
3. Implement the changes.
4. Execute all tests.
5. Verify that the project builds without warnings.
6. Commit your changes.
7. Push your branch.
8. Open a Pull Request.

Example:

```bash
git checkout -b feature/improve-validation
```

---

# Coding Standards

The project follows modern C# conventions.

## Language

- Latest supported C# version.
- Nullable Reference Types enabled.
- File-scoped namespaces.
- Implicit usings enabled.

---

## Naming

Public members use PascalCase.

Private fields use:

```csharp
_privateField
```

Interfaces begin with:

```
I
```

Generic type parameters should use meaningful names.

Avoid abbreviations whenever possible.

---

## XML Documentation

Every public type must include XML documentation.

Example:

```csharp
/// <summary>
/// Represents a strongly typed customer identifier.
/// </summary>
public sealed class CustomerId
{
}
```

Pull Requests introducing undocumented public APIs will not be accepted.

---

## Immutability

Prefer immutable models.

Value Objects should always be immutable.

Collections exposed publicly should be read-only whenever possible.

---

## Exceptions

Business validation should use:

- Result
- ValidationResult
- Maybe

Avoid using exceptions for expected business failures.

Exceptions should represent programming errors or exceptional situations only.

---

## Strongly Typed Models

Avoid primitive obsession.

Prefer:

```csharp
CustomerId
```

instead of

```csharp
Guid
```

Prefer:

```csharp
Email
```

instead of

```csharp
string
```

---

# Module Responsibilities

Every module has exactly one responsibility.

Do not introduce code that mixes responsibilities.

Examples:

| Module         | Responsibility              |
|----------------|-----------------------------|
| Results        | Functional result model     |
| Validation     | Validation model            |
| Domain         | DDD building blocks         |
| DomainEvents   | Domain event infrastructure |
| Specifications | Specification Pattern       |
| Time           | Time abstractions           |
| Globalization  | Culture abstractions        |
| Versioning     | Semantic Versioning         |

---

# Frozen Modules

Some modules are considered architecturally frozen.

Changes to these modules require a strong architectural justification.

Frozen modules include:

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

Breaking changes to frozen modules are discouraged.

---

# Public API

The public API is considered part of the framework contract.

Avoid:

- unnecessary public classes;
- unnecessary public methods;
- exposing implementation details.

Whenever possible, prefer:

```csharp
internal
```

instead of

```csharp
public
```

---

# Backward Compatibility

Backward compatibility is a fundamental project goal.

Public APIs should not change without a compelling reason.

Breaking changes are reserved for future major releases.

---

# Testing

Every contribution should include tests whenever appropriate.

Tests should be:

- deterministic;
- isolated;
- repeatable;
- independent.

Avoid tests depending on:

- current date/time;
- external services;
- network access.

Use `FakeClock` whenever testing time-dependent behavior.

---

# Dependencies

Avoid introducing external dependencies.

The Shared Kernel intentionally depends almost exclusively on the .NET Base Class Library.

Any new dependency should provide significant architectural value.

---

# Code Review

Every Pull Request is reviewed from multiple perspectives.

Review criteria include:

- correctness;
- readability;
- simplicity;
- architectural consistency;
- maintainability;
- performance.

---

# Commit Messages

Use clear commit messages.

Good examples:

```
Improve ValidationResult API

Fix StructuralComparer equality

Add SemanticVersion parsing

Improve XML documentation
```

Avoid generic messages such as:

```
Fix

Update

Changes

Misc
```

---

# Performance

Performance improvements are welcome.

However:

- readability takes precedence over micro-optimizations;
- benchmark results should accompany significant optimizations.

---

# Documentation

Documentation is considered part of the source code.

Whenever new public APIs are introduced:

- XML documentation must be updated.
- README examples should be updated when applicable.
- Additional documentation should be added if necessary.

---

# Discussions

Architectural proposals should be discussed before implementation.

This helps preserve long-term consistency across the framework.

---

# Reporting Bugs

Please include:

- framework version;
- .NET version;
- operating system;
- reproduction steps;
- expected behaviour;
- actual behaviour.

Small reproducible examples are greatly appreciated.

---

# Feature Requests

Feature requests should explain:

- the problem being solved;
- why existing APIs are insufficient;
- the proposed solution;
- possible alternatives.

---

# Thank You

Every contribution helps improve the quality of KUKULCAN.SharedKernel.i18n.

Thank you for helping build a stable, maintainable and long-lived architectural foundation for the KUKULCAN ecosystem.
