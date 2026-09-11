# API

> **KUKULCAN.SharedKernel.i18n**  
> **HTTP API Reference**

---

# 1. API Overview

The service exposes a versioned REST API under `/api/v1`.

Controllers are intentionally thin. Each endpoint creates a MediatR command/query and maps the application result to an HTTP response.

Responses use JSON when a response body is defined. Successful `204 No Content` responses have no response body. Application/domain failures are returned as RFC 7807 `ProblemDetails` with the domain/application error code also exposed as the `errorCode` extension.

---

# 2. Authentication and Authorization

The API uses JWT Bearer authentication.

Two authorization policies are defined by the service:

| Policy | Purpose |
|---|---|
| `i18n.read` | Authenticated read operations |
| `i18n.write` | Administrative create/update/delete operations |

The write policy is intended for the platform administration roles described by the repository configuration, including `KUKULCAN.Admin` and `KUKULCAN.i18n.Admin`.

Requests requiring authorization return `401 Unauthorized` when authentication is missing or invalid, and `403 Forbidden` when the caller is authenticated but does not satisfy the required policy.

---

# 3. Translations

Base route:

```text
/api/v1/translations
```

| Method | Route | Policy | Purpose |
|---|---|---|---|
| GET | `/{code}/{languageCode}` | `i18n.read` | Resolve one translation with fallback |
| GET | `/module/{module}/{languageCode}` | `i18n.read` | Return a module dictionary |
| GET | `/` | `i18n.write` | Administrative paged list |
| GET | `/{code}/variants` | `i18n.write` | List language variants |
| POST | `/` | `i18n.write` | Create translation |
| PUT | `/{code}/{languageCode}` | `i18n.write` | Update text/context |
| PATCH | `/{code}/{languageCode}/review` | `i18n.write` | Set review status |
| DELETE | `/{code}/{languageCode}` | `i18n.write` | Delete non-protected translation |
| POST | `/bulk` | `i18n.write` | Bulk upsert, up to 5,000 entries |

## Lookup

Example:

```http
GET /api/v1/translations/CRM0001/es-MX
```

The lookup service walks the BCP-47 fallback hierarchy. A regional language can fall back to its parent language and then to the configured global default.

The response exposes `isFallback` and the language actually resolved by the lookup, allowing clients to distinguish an exact translation from a fallback result.

The endpoint is designed as a hot path and is cached.

---

# 4. Languages

Base route:

```text
/api/v1/languages
```

| Method | Route | Policy | Purpose |
|---|---|---|---|
| GET | `/` | `i18n.read` | List languages |
| GET | `/{code}` | `i18n.read` | Get one language |
| POST | `/` | `i18n.write` | Create language |
| PUT | `/{code}` | `i18n.write` | Update display names |
| PATCH | `/{code}/active` | `i18n.write` | Activate/deactivate |
| PATCH | `/{code}/default` | `i18n.write` | Set global default |

`GET /api/v1/languages` accepts `activeOnly=true` by default. Administrators can request inactive languages by setting it to `false`.

Languages are global and are never physically deleted; there is no language-delete endpoint. A language is deactivated/reactivated through the active-state endpoint instead.

The default language cannot be deactivated. Setting another active language as default transfers the default designation from the previous default; the newly selected default is active by definition.

Language creation validates the code as BCP-47 and requires non-empty display names; application validation limits `Name` and `NativeName` to 100 characters. Language update requires non-empty display names, but the current `UpdateLanguageCommand` has no application maximum-length validator.

---

# 5. Locales

Base route:

```text
/api/v1/locales
```

| Method | Route | Policy | Purpose |
|---|---|---|---|
| GET | `/` | `i18n.read` | List all locale configurations |
| GET | `/{languageCode}` | `i18n.read` | Get one configuration |
| PUT | `/{languageCode}` | `i18n.write` | Create/update configuration |

Locale configuration includes date/time formats, first day of week, decimal/thousands separators and decimal precision.

The application validates the language code as BCP-47; `DateFormat`, `ShortDateFormat` and `TimeFormat` are required and limited to 50 characters; `DateTimeFormat` is required and limited to 100 characters; `FirstDayOfWeek` must be a supported enum value; decimal and thousands separators are single, non-empty and distinct characters; and both decimal-precision fields must be in the range `0..10`.

A locale configuration can only be created or updated for an existing language.

---

# 6. Currencies

Base route:

```text
/api/v1/currencies/{languageCode}
```

| Method | Route | Policy | Purpose |
|---|---|---|---|
| GET | `/` | `i18n.read` | List formats for a language |
| PUT | `/{currencyCode}` | `i18n.write` | Create/update a format |
| DELETE | `/{currencyCode}` | `i18n.write` | Delete a format |

The configuration controls symbol placement, spacing, separators, decimal places and negative-number formatting.

Currency upsert validates the language code as BCP-47 and the currency code as exactly three letters. `CurrencyName` is required and limited to 100 characters; `Symbol` is required and limited to 5 characters; `SymbolPosition` must be a supported enum value; decimal and thousands separators are single, non-empty and distinct characters; decimal places must be in the range `0..10`; and `NegativePattern` must contain the `{amount}` placeholder.

Currency upsert requires the referenced language to exist. Deleting a missing currency format returns `404 Not Found`.

---

# 7. HTTP Status Semantics

The HTTP status is derived from the application/domain error code by the API result mapping. It is not inferred from the human-readable error message.

Common responses include:

| Status | Meaning |
|---|---|
| `200 OK` | Successful query or update operation |
| `201 Created` | Successful language/translation creation |
| `204 No Content` | Successful state change or delete without response body |
| `401 Unauthorized` | Authentication is missing or invalid |
| `403 Forbidden` | Caller is authenticated but does not satisfy the required authorization policy |
| `404 Not Found` | Requested language, translation, locale configuration or currency format does not exist |
| `409 Conflict` | Operation violates a business or uniqueness constraint, including inactive-default selection, default-language deactivation, duplicate language/translation, or protected translation deletion |
| `422 Unprocessable Entity` | Request passed transport parsing but failed application/domain validation |
| `500 Internal Server Error` | Error code is not mapped to one of the known domain/application HTTP categories |

Errors are represented as RFC 7807 `ProblemDetails`. The domain/application error code is also returned in the `errorCode` extension.

---

# 8. Translation Fallback Contract

The fallback algorithm is intentionally deterministic:

```text
requested language
      |
      v
exact translation
      |
      +-- found --> return exact
      |
      +-- missing --> parent BCP-47 language
                           |
                           +-- found --> return parent
                           |
                           +-- missing --> configured global default language
                                                |
                                                +-- found --> return fallback
```

For example:

```text
Request: es-MX
   |
   +--> es-MX
   +--> es
   +--> configured default
```

The client does not need to implement this algorithm itself. The lookup response identifies whether fallback occurred and reports the actual language used.

---

# 9. Administration Rules

The administrative API is deliberately more restrictive than runtime lookup.

Important protections include:

- Languages are never physically deleted.
- The default language cannot be deactivated.
- Setting an inactive language as the default is rejected; the language must be active first.
- Default-language translation deletion is protected by the service's translation rules.
- Updating translation text clears its reviewed status.
- Bulk operations are bounded to a maximum of 5,000 entries.

---

# 10. API Documentation

The API project generates XML documentation and exposes OpenAPI/Scalar support. Scalar is intended as the interactive developer experience for the versioned API.

Typical development endpoint:

```text
/scalar/v1
```

The exact HTTP/HTTPS base address is host/environment dependent.

---

# 11. Health Endpoints

The host exposes:

```text
/health
/health/live
/health/ready
```

The global endpoint reports the configured health checks. Liveness verifies that the process is running, while readiness verifies the external dependencies required for serving traffic, notably PostgreSQL and Redis when Redis is configured as a readiness dependency.
