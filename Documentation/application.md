# Application

> **KUKULCAN.SharedKernel.i18n**  
> **Application Layer Reference**

---

# 1. Purpose

The Application project implements the service's use cases. It coordinates domain objects and repository contracts through MediatR and keeps HTTP, EF Core and infrastructure implementation details outside the handlers.

The feature folders are:

```text
Features/
├── Currencies/
├── Languages/
├── Locales/
└── Translations/
```

---

# 2. CQRS Model

Commands change state; queries retrieve state.

```text
HTTP request
    |
    v
Controller
    |
    v
IMediator
    |
    +---- Command ----> Validator -> Handler -> Domain/Repository
    |
    +---- Query ------> Validator -> Handler -> Repository/Cache
```

This separation makes read-heavy translation lookup independent from administrative write workflows.

---

# 3. Translation Commands

The translation command set includes:

| Command | Responsibility |
|---|---|
| `CreateTranslationCommand` | Creates a translation for a code/language pair |
| `UpdateTranslationCommand` | Changes translation text/context and resets review state |
| `SetTranslationReviewedCommand` | Marks or unmarks a translation as human reviewed |
| `DeleteTranslationCommand` | Removes a non-protected translation |
| `BulkUpsertTranslationsCommand` | Inserts or updates a large translation set |

The bulk operation is intended for imports and CI/CD pipelines and supports up to 5,000 entries per request according to the API contract.

---

# 4. Translation Queries

| Query | Responsibility |
|---|---|
| `GetTranslationQuery` | Resolves one translation using fallback |
| `GetTranslationsByModuleQuery` | Returns a complete module dictionary |
| `GetTranslationsPagedQuery` | Provides administrative pagination/filtering |
| `GetTranslationVariantsQuery` | Lists language variants for one code |

The single-translation query is the hot path. It is designed to return a resolved value together with fallback information.

---

# 5. Language Features

Language commands include:

- `CreateLanguageCommand`
- `UpdateLanguageCommand`
- `SetLanguageActiveCommand`
- `SetDefaultLanguageCommand`

Queries include:

- `GetAllLanguagesQuery`
- `GetLanguageQuery`

The application layer ensures that lifecycle operations are routed through the domain model instead of changing aggregate state directly from controllers.

---

# 6. Locale Features

The locale feature provides:

- `GetAllLocaleConfigurationsQuery`
- `GetLocaleConfigurationQuery`
- `UpsertLocaleConfigurationCommand`

Upsert semantics allow a language's configuration to be created or replaced through one operation.

---

# 7. Currency Features

The currency feature provides:

- `GetCurrencyFormatsQuery`
- `UpsertCurrencyFormatCommand`
- `DeleteCurrencyFormatCommand`

Currency formats are addressed by the combination of language code and currency code.

---

# 8. Validation

The project uses FluentValidation for application command/query validation. Validation belongs at the use-case boundary because it concerns request shape and application constraints, while aggregate invariants remain in the domain model.

Typical validation responsibilities include:

- Required fields.
- Maximum lengths.
- Valid language and translation code formats.
- Pagination bounds.
- Bulk-operation size limits.
- Valid combinations of formatting properties.

The distinction is important:

```text
Application validation = "Can this request enter the use case?"
Domain invariant        = "Can this state exist in the model?"
```

---

# 9. Pipeline Behaviors

The application project contains MediatR behaviors for cross-cutting use-case concerns.

These behaviors should remain generic and should not contain feature-specific business rules. Logging, validation and similar policies belong in the pipeline so that handlers remain focused on the use case itself.

---

# 10. Pagination

Administrative translation listing uses `PaginationRequest` and `PagedResult<T>`.

The API exposes:

- `page`
- `pageSize`
- `module`
- `languageCode`
- `sortBy`

The default page is `1` and the default page size is `50`.

Pagination is deliberately restricted to administrative tooling; normal runtime lookup uses direct code/language access because that is substantially cheaper for clients.

---

# 11. Result Mapping

Handlers return application/domain results that are converted to HTTP responses by API extensions. This keeps HTTP status-code decisions out of application handlers.

For example:

```text
Handler failure
      |
      v
Application Result
      |
      v
API mapping extension
      |
      v
ProblemDetails / HTTP status
```

---

# 12. Registration

`ApplicationServiceRegistration` is responsible for registering MediatR handlers, validators and application-level services with dependency injection.

The composition root belongs to the API/host, while the Application project exposes the registration entry point required by the host.

---

# 13. Application Design Rules

1. Handlers must not depend on ASP.NET controllers.
2. Handlers should depend on abstractions rather than concrete repositories.
3. Validation should not duplicate domain invariants unnecessarily.
4. Queries should not mutate domain state.
5. Commands should make state changes through domain behavior.
6. Cross-cutting concerns should use pipeline behaviors when practical.
7. DTOs and request contracts should not become domain entities.

---

# 14. Typical Request Flow

A translation lookup follows this sequence:

```text
GET /api/v1/translations/CRM0001/es-MX
        |
        v
TranslationsController
        |
        v
GetTranslationQuery
        |
        v
MediatR pipeline
        |
        v
GetTranslationQueryHandler
        |
        +--> cache lookup
        |
        +--> TranslationLookupService
        |
        +--> repository when needed
        |
        v
TranslationLookupDto
        |
        v
HTTP 200
```

An administrative write follows the equivalent command path and uses the write authorization policy before the request reaches the application layer.
