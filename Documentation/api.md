# API

> **KUKULCAN.SharedKernel.i18n**  
> **HTTP API Reference**

---

# 1. API Overview

The service exposes a versioned REST API under `/api/v1`.

Controllers are intentionally thin. Each endpoint creates a MediatR command/query and maps the application result to an HTTP response.

All responses use JSON, and failures are exposed as `ProblemDetails` where the controller declares an error response.

---

# 2. Authentication and Authorization

The API uses JWT Bearer authentication.

Two authorization policies are defined by the service:

| Policy | Purpose |
|---|---|
| `i18n.read` | Authenticated read operations |
| `i18n.write` | Administrative create/update/delete operations |

The write policy is intended for the platform administration roles described by the repository configuration, including `KUKULCAN.Admin` and `KUKULCAN.i18n.Admin`.

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

The response exposes an `isFallback` indicator so clients can distinguish an exact translation from a fallback result.

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

The default language cannot be deactivated.

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

---

# 7. HTTP Status Semantics

The API uses normal REST semantics where the controller declares the corresponding response type.

Common responses include:

| Status | Meaning |
|---|---|
| `200 OK` | Successful query/update operation |
| `201 Created` | Successful creation |
| `204 No Content` | Successful state change/delete without response body |
| `404 Not Found` | Requested language/translation/configuration does not exist |
| `409 Conflict` | Operation violates a business or uniqueness constraint |
| `422 Unprocessable Entity` | Request passed transport parsing but failed validation |

Errors are represented as `ProblemDetails` where applicable.

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
                           +-- missing --> global default language
                                                |
                                                +-- found --> return fallback
```

For example:

```text
Request: es-MX
   |
   +--> es-MX
   +--> es
   +--> configured default (normally en)
```

The client does not need to implement this algorithm itself.

---

# 9. Administration Rules

The administrative API is deliberately more restrictive than runtime lookup.

Important protections include:

- The default language cannot be deactivated.
- Default-language translation deletion is protected by the service's translation rules.
- Updating translation text clears its reviewed status.
- Bulk operations are bounded to protect the service from unbounded request payloads.

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
