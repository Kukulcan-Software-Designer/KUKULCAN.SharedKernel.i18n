# Domain

> **KUKULCAN.SharedKernel.i18n**  
> **Domain Model Reference**

---

# 1. Purpose

The Domain project defines the language of the internationalization service. It contains the concepts and invariants that must remain valid regardless of whether the service is called through HTTP, executed by a background process or persisted by EF Core.

The principal concepts are languages, translations, locale configurations and currency formats.

---

# 2. Aggregates and Entities

| Type | Role | Main responsibility |
|---|---|---|
| `Language` | Aggregate root | Defines a supported language and its lifecycle/default status |
| `Translation` | Aggregate root | Stores one translation for one code/language pair |
| `LocaleConfiguration` | Entity | Defines date, time and numeric conventions for a language |
| `CurrencyFormat` | Entity | Defines currency presentation rules for a language |

`Language` and `Translation` are the principal aggregate roots because application operations address them as independent consistency boundaries.

---

# 3. Language

A language is identified by a strongly typed `LanguageCode` and contains both display information and runtime configuration state.

Important rules include:

- A language code must be valid according to the module's language-code rules.
- Languages can be activated or deactivated.
- The default language must remain active.
- Setting a new default transfers the default designation from the previous default.
- The service uses the default language as the final fallback when an exact or parent BCP-47 language cannot resolve a translation.

Typical platform languages may include `en`, `es`, `ca`, `fr` and `de`; the actual seed set is controlled by the repository's seed configuration.

---

# 4. Translation

A translation is identified by the combination of:

```text
TranslationCode + LanguageCode
```

The code convention is:

```text
{MODULE}{NNNN}
```

Examples:

```text
CRM0001
CRM0100
AUTH0010
CORE0001
```

The module prefix is uppercase and identifies the owning platform module. The numeric suffix is a four-digit sequence.

A translation contains the translated text and optional contextual information. Updating text resets the human-reviewed state so that changed text can be reviewed again.

The default-language translation is treated as the base translation for fallback and receives additional deletion protection.

---

# 5. Language Codes

`LanguageCode` is a value object that prevents primitive-string language codes from leaking through the domain model.

The API accepts BCP-47-style identifiers. The lookup path can therefore distinguish exact regional languages such as:

```text
es-MX
es-ES
en-US
```

from their parent language:

```text
es
en
```

This distinction is important to fallback resolution.

---

# 6. Translation Codes

`TranslationCode` is a value object responsible for validating and representing the platform translation-code convention.

The code should be treated as a stable contract. Consumers should not infer database identifiers from it or generate arbitrary values at runtime.

A recommended allocation strategy is to reserve ranges by module and purpose, for example:

| Range | Example purpose |
|---|---|
| `0001-0099` | Entity/domain labels |
| `0100-0199` | Error messages |
| `0200-0299` | UI labels |
| `0300-0399` | Validation messages |

The ranges are organizational conventions; the value object remains responsible for structural validity.

---

# 7. Locale Configuration

`LocaleConfiguration` centralizes formatting rules that otherwise tend to become hard-coded in consuming applications.

It includes:

- Long date format.
- Short date format.
- Time format.
- Date/time format.
- First day of week.
- Decimal separator.
- Thousands separator.
- Decimal places.
- Currency decimal places.

This model deliberately describes formatting metadata rather than performing formatting itself.

---

# 8. Currency Format

`CurrencyFormat` associates a currency with a language and describes how amounts are rendered.

The configuration covers:

- Currency name.
- Symbol.
- Symbol position.
- Whether a space separates symbol and amount.
- Decimal separator.
- Thousands separator.
- Decimal places.
- Negative-number pattern.

A response may include a formatted example to make administrative configuration easier to verify.

---

# 9. Domain Services

## `LanguageDomainService`

Encapsulates language-level operations that span more than one language instance, particularly default-language transfer rules.

## `TranslationLookupService`

Coordinates translation resolution using the requested language and fallback hierarchy. The lookup service is distinct from persistence so that fallback behavior remains an explicit business capability.

## Cache Services

The repository currently contains `DistributedCacheService` and `MemoryOnlyCacheService` in the Domain project. They are technical abstractions/implementations used by the module's caching path rather than domain entities. Their placement should be treated as an implementation detail and kept away from the aggregate invariants.

---

# 10. Repository Contracts

Repository interfaces belong to the inner layer and are implemented in Infrastructure.

This allows application handlers and domain services to request persistence operations without knowing whether data is stored in PostgreSQL, another relational provider or a test double.

The implementation must not move query-specific business rules into repositories when those rules belong to the domain or application layer.

---

# 11. Domain Errors

Typed domain errors are used for invalid state transitions and business conflicts. The API later maps application/domain failures to HTTP responses and `ProblemDetails`.

Examples of business conflicts include attempting to deactivate the default language or attempting an operation that violates the translation uniqueness rules.

---

# 12. Domain Events

The module follows the Shared Kernel domain-event model. Domain events provide a mechanism for significant state changes to be propagated without coupling aggregates to external infrastructure.

Examples described by the service model include:

- `LanguageCreatedEvent`
- `TranslationCreatedEvent`
- `TranslationTextUpdatedEvent`

Infrastructure is responsible for dispatching events after successful persistence when the shared persistence mechanism is configured to do so.

---

# 13. Invariants Summary

| Invariant | Enforcement point |
|---|---|
| Translation code has valid structure | `TranslationCode` |
| Language code has valid structure | `LanguageCode` |
| Default language remains active | Language command/domain rules |
| Default language can be transferred | `LanguageDomainService` / application command |
| Default-language translations are protected | Translation deletion rules |
| Updated translations require re-review | Translation update behavior |
| Fallback is deterministic | `TranslationLookupService` |

---

# 14. Domain Usage Rule

Application and API code should consume domain concepts rather than reproducing their validation rules with ad-hoc string checks.

For example, code should construct a `TranslationCode` rather than independently validating whether a string happens to look like `CRM0001`.

This keeps the ubiquitous language centralized and prevents different adapters from implementing subtly different rules.
