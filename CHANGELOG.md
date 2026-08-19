# Changelog

All notable changes to **KUKULCAN.SharedKernel.i18n** will be documented in this file.

The format of this document follows the principles of **Keep a Changelog** and the project adheres to **Semantic Versioning 2.0.0**.

- Keep a Changelog: https://keepachangelog.com/en/1.1.0/
- Semantic Versioning: https://semver.org/

---

# Versioning Policy

This project uses the following versioning strategy:

- **Major** versions introduce incompatible API changes.
- **Minor** versions introduce new features while preserving backward compatibility.
- **Patch** versions contain only backward-compatible bug fixes.

Pre-release versions are identified using the Semantic Versioning specification.

Examples:

```
1.0.0-alpha1
1.0.0
1.0.0-rc1
1.0.0
```

---

# Types of Changes

This changelog classifies modifications using the following categories.

## Added

New features.

## Changed

Changes to existing functionality.

## Deprecated

Features scheduled for removal.

## Removed

Features removed from the framework.

## Fixed

Bug fixes.

## Security

Security improvements.

---

# [Unreleased]

## Added

- Work in progress.

---

# [1.0.0] - 2026-07-30

## Added

### Architecture

- Initial public architecture.
- Modular Shared Kernel design.
- Complete architectural audit.
- Frozen public API.
- Clean Architecture support.
- Domain-Driven Design building blocks.

### Results

- Functional Result pattern.
- Generic Result<T>.
- Rich Error model.
- CommonErrors.
- CommonErrorCodes.
- Error metadata support.
- Error composition helpers.

### Maybe

- Optional value representation.
- Null-free API.
- Functional optional pattern.

### Guards

- Argument validation helpers.
- Consistent guard clause API.

### Validation

- ValidationResult.
- ValidationFailure.
- ValidationExtensions.
- ValidationThrowExtensions.
- ValidationException.
- Integration with Result.

### Domain

- Entity.
- AggregateRoot.
- ValueObject.
- Enumeration.
- AuditableEntity.
- Domain model abstractions.

### Domain Events

- DomainEvent.
- IDomainEvent.
- Aggregate event collection.
- Event management infrastructure.

### Specifications

- Specification Pattern.
- Composite specifications.
- Expression-based specifications.

### Identifiers

- Strongly typed identifiers.
- EntityId<T> base implementation.

### Time

- IClock abstraction.
- SystemClock.
- FakeClock.
- Deterministic testing support.

### Globalization

- SupportedCulture.
- Culture parsing.
- Localization abstractions.
- Formatting abstractions.
- Localized models.

### Versioning

- SemanticVersion.
- Semantic Versioning 2.0 support.
- Parsing.
- Comparison.
- Validation.

### Internals

- StructuralComparer.
- DictionaryComparer.
- EnumerableComparer.
- ObjectFormatter.

### Documentation

- Complete XML documentation.
- GitHub README.
- Architecture documentation.
- Usage examples.
- Mermaid diagrams.
- Public API documentation.

---

## Changed

### General

- Entire framework reviewed module by module.
- Consistent naming across all modules.
- Public API simplified.
- Internal implementation reorganized.
- Architectural responsibilities clarified.

### Results

- Final Error model.
- Final Result API.
- Improved error consistency.

### Validation

- Final ValidationResult implementation.
- Improved Result integration.
- Validation pipeline simplified.

### Domain

- AggregateRoot event management refined.
- Enumeration finalized.
- Entity model stabilized.

### Globalization

- Unified culture abstractions.
- Formatting interfaces aligned.

### Versioning

- SemanticVersion redesigned as immutable Value Object.

---

## Removed

### Architecture

- Architectural inconsistencies discovered during the audit.
- Duplicate responsibilities.
- Experimental APIs not aligned with the final architecture.

### Validation

- Obsolete validation concepts.
- Redundant validation helpers.

### Results

- Legacy error patterns.
- Inconsistent helper methods.

---

## Fixed

### General

- XML documentation inconsistencies.
- Nullable reference warnings.
- Internal equality behavior.
- Structural comparison issues.

### Results

- Error propagation.
- Result consistency.
- Generic Result behavior.

### Validation

- Conversion between ValidationResult and Result.
- Validation extension behavior.

### Internals

- StructuralComparer implementation.
- DictionaryComparer implementation.
- EnumerableComparer implementation.

### Time

- FakeClock behaviour.
- Time manipulation methods.

---

## Security

No known security vulnerabilities at the time of this release.

---

# Upgrade Notes

This is the first public beta release.

No migration steps are required.

---

# Compatibility

| Component                | Status    |
|--------------------------|-----------|
| .NET 10                  | Supported |
| Nullable Reference Types | Enabled   |
| File Scoped Namespaces   | Enabled   |
| Clean Architecture       | Supported |
| Domain-Driven Design     | Supported |
| Semantic Versioning 2.0  | Supported |

---

# Frozen Modules

The following modules are considered architecturally frozen for **v1.0.0**.

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

Future changes to these modules will prioritize backward compatibility.

---

# Release Summary

**KUKULCAN.SharedKernel v1.0.0** represents the first public beta of the framework.

This release provides a stable architectural foundation for building enterprise applications following Domain-Driven Design and Clean Architecture.

The public API has been reviewed, audited and frozen to provide a predictable and maintainable development experience.

Future releases will focus on:

- Bug fixes.
- Performance improvements.
- Documentation.
- Additional unit tests.
- New features without breaking the public API.
