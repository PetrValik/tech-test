# Order API

A RESTful microservice for managing customer orders, written in C# (.NET 8) using a classic **Layered Architecture** pattern.

---

## Architecture Overview

```
HTTP Request
    └─► OrderController          (Order.WebAPI  – MVC controller)
            └─► IOrderService    (Order.Service – business logic)
                    └─► IOrderRepository  (Order.Data – data access)
                                └─► OrderContext  (EF Core → MySQL)
```

Each layer has a single responsibility and communicates only with the layer directly below it through an interface:

| Layer | Project | Responsibility |
|---|---|---|
| **HTTP** | `Order.WebAPI` | Route requests, validate inputs (FluentValidation), return HTTP responses |
| **Service** | `Order.Service` | Business logic, orchestrate repository calls |
| **Repository** | `Order.Data` | EF Core queries, entity → DTO projection |
| **Persistence** | MySQL (Docker) | Relational data store |

---

## Project Structure

```
tech-test/
├── src/                        V1 — Enterprise Layered Architecture (.NET 8)
│   ├── Order.Data/             EF Core DbContext, entities, OrderRepository
│   ├── Order.Model/            Shared DTOs and request/response models
│   ├── Order.Service/          IOrderService + OrderService implementation
│   ├── Order.Service.Tests/    NUnit unit tests (37 tests, SQLite in-memory)
│   ├── Order.API.Tests/        NUnit integration tests (28 tests, WebApplicationFactory)
│   ├── Order.WebAPI/           MVC controllers, FluentValidation validators, Startup
│   ├── docs/api/               Per-endpoint API documentation
│   ├── Dockerfile
│   └── Order.sln
├── src-v2/                     V2 — Vertical Slice / MediatR / Minimal API (.NET 10)
│   ├── OrderApi/               Main application (Features, Infrastructure, Middleware)
│   ├── OrderApi.Tests/         xUnit tests (83 tests, per-feature split)
│   ├── Dockerfile
│   └── OrderApi.slnx
├── database/
│   └── init.sql                V1 schema + seed data (shared compose)
├── docker-compose.yml          Run V1 + V2 together (ports 8000 + 8001)
├── docker-compose.v1.yml       Run V1 only (port 8000)
├── docker-compose.v2.yml       Run V2 only (port 8001)
└── .env.example                Copy to .env and set DB_PASSWORD before first run
```

---

## Technology Stack

| Concern | Choice | Version |
|---|---|---|
| Runtime | .NET | 8.0 LTS |
| Web framework | ASP.NET Core MVC | 8.0 |
| ORM | Pomelo EF Core MySQL | 8.0.x |
| Validation | FluentValidation | 11.x |
| API docs | Swashbuckle (Swagger UI) | 6.x |
| Health check | Built-in ASP.NET Core health checks | – |
| Unit tests | NUnit 4 + EF Core SQLite in-memory | 4.x |
| Integration tests | NUnit 4 + WebApplicationFactory + SQLite | 4.x |
| Database | MySQL | 5.7 (Docker) |

---

## Getting Started

### Prerequisites

| Requirement | Version | Used by |
|---|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | any (Linux containers) | Both |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 LTS | V1 (`src/`) |
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 | V2 (`src-v2/`) |

### Run with Docker

Three compose files are available depending on what you want to run:

```bash
# Prerequisites: copy .env.example to .env and set DB_PASSWORD
cp .env.example .env   # Windows: copy .env.example .env

# Both V1 + V2 side by side (V1 → port 8000, V2 → port 8001)
docker compose up

# V1 only
docker compose -f docker-compose.v1.yml up

# V2 only (includes Redis for output caching)
docker compose -f docker-compose.v2.yml up
```

| Version | API URL | API Docs | README |
|---------|---------|----------|--------|
| V1 | http://localhost:8000 | http://localhost:8000/swagger (dev only) | [src/README.md](src/README.md) |
| V2 | http://localhost:8001 | http://localhost:8001/scalar/v1 (dev only) | [src-v2/README.md](src-v2/README.md) |

### Run locally (Development)

```bash
# Start only the database
docker compose up db

# Run V1 (from the src/ folder)
cd src
dotnet run --project Order.WebAPI

# Run V2 (from the src-v2/ folder)
cd src-v2
dotnet run --project OrderApi
```

### Run tests

No Docker required — tests use SQLite in-memory.

```bash
cd src
dotnet test
```

---

## API Endpoints

| Method | Route | Description | Docs |
|---|---|---|---|
| `GET` | `/orders` | List all orders (newest first) | [GetOrders](src/docs/api/GetOrders.md) |
| `GET` | `/orders/{orderId}` | Get a single order with all items | [GetOrderById](src/docs/api/GetOrderById.md) |
| `GET` | `/orders/status/{statusName}` | Filter orders by status name | [GetOrdersByStatus](src/docs/api/GetOrdersByStatus.md) |
| `POST` | `/orders` | Create a new order | [CreateOrder](src/docs/api/CreateOrder.md) |
| `PATCH` | `/orders/{orderId}/status` | Update an order's status | [UpdateOrderStatus](src/docs/api/UpdateOrderStatus.md) |
| `GET` | `/orders/profit/monthly` | Monthly profit for Completed orders | [GetMonthlyProfit](src/docs/api/GetMonthlyProfit.md) |
| `GET` | `/health/live` | Liveness probe — always 200, no dependencies | [Health](src/docs/api/Health.md) |
| `GET` | `/health/ready` | Readiness probe — checks database connectivity | [Health](src/docs/api/Health.md) |

---

## Secrets Management

All sensitive values are supplied via a `.env` file at the repository root. A template is committed as `.env.example`:

```bash
cp .env.example .env   # Windows: copy .env.example .env
# Edit .env and set DB_PASSWORD (and optionally JWT_AUTHORITY, REDIS_CONNECTION_STRING)
```

Key variables:

| Variable | Used by | Purpose |
|---|---|---|
| `DB_PASSWORD` | Both versions | MySQL root + app-user password |
| `JWT_AUTHORITY` | Both versions | IdP issuer URL (empty = auth not enforced) |
| `JWT_AUDIENCE` | Both versions | Expected JWT audience claim |
| `REDIS_CONNECTION_STRING` | V2 | Redis output cache (empty = in-memory fallback) |
| `AZURE_MONITOR_CONNECTION_STRING` | V2 | OpenTelemetry exporter (empty = console) |



## Domain Glossary

| Term | Meaning |
|---|---|
| **Reseller** | A company that is a customer of Giacom |
| **Customer** | An end-customer of a Reseller |
| **Order** | An order placed by a Reseller for a specific Customer |
| **Order Item** | A product line within an Order |
| **Product** | A purchasable offering (e.g. "100GB Mailbox") |
| **Service** | The category a Product belongs to (e.g. "Email") |
| **Order Status** | The lifecycle state of an Order: `Created`, `In Progress`, `Failed`, `Completed` |
| **Profit** | Price − Cost per item; aggregated per calendar month |

---

## Database Credentials (local Docker)

| Setting | Value |
|---|---|
| Hostname | `localhost` |
| Port | `3306` |
| Username | `order-service` |
| Password | `nmCsdkhj20n@Sa` |

---

## Key Design Decisions

- **Client-side aggregation for totals**: `TotalCost`/`TotalPrice` sums are computed in C# after EF Core loads order items, ensuring compatibility with both MySQL (production) and SQLite (tests). EF Core 8 cannot translate `decimal` aggregates to SQLite SQL.
- **Binary GUID storage**: MySQL stores GUIDs as `binary(16)` for storage efficiency. The repository includes a `IsInMemory()` guard for ID comparisons to keep tests working with SQLite.
- **FluentValidation**: Validators live in `Order.WebAPI/Validators/` and are registered via `AddValidatorsFromAssemblyContaining<Startup>()` with automatic model-state integration.
- **204 No Content on status update**: `PATCH /orders/{id}/status` returns `204 No Content` (not `200 OK`) per REST semantics for mutations that return no body.

---

## v2 – Vertical Slice Architecture (src-v2/)

> **`src-v2/`** is a parallel implementation of the same API built from scratch in **.NET 10** using a **Vertical Slice Architecture**. It is intended as a clean-code reference showing how the same domain can be structured very differently — each feature is self-contained and owns everything it needs.

### Architecture

```
HTTP Request
    └─► IEndpoint.MapEndpoint()  (one class per route, discovered by assembly scan)
                └─► Handler              (Features/Orders/<Feature>/Handler.cs)
                            └─► OrderContext  (EF Core → MySQL)
```

No controller, no service layer, no repository — each feature slice talks directly to the database context.

### Project Structure (src-v2/)

```
src-v2/
├── OrderApi/
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs     Auto-validates all MediatR requests
│   │   │   └── LoggingBehavior.cs        Logs every request/response via Serilog
│   │   ├── Endpoints/
│   │   │   ├── IEndpoint.cs              Contract — each feature endpoint implements MapEndpoint()
│   │   │   └── EndpointExtensions.cs     AddEndpoints() scans assembly; MapEndpoints() wires routes
│   │   └── Events/
│   │       ├── IOrderEventPublisher.cs   Domain event hook (outbox pattern)
│   │       ├── NullOrderEventPublisher.cs Null impl — swap for real broker in prod
│   │       ├── OrderCreatedEvent.cs      Event raised when a new order is created
│   │       ├── OrderDeletedEvent.cs      Event raised when an order is soft-deleted
│   │       └── OrderStatusChangedEvent.cs Event raised when order status changes
│   ├── Exceptions/
│   │   └── GlobalExceptionHandler.cs    Consistent JSON error responses
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs  Focused bootstrap methods (AddPersistence, etc.)
│   │   └── WebApplicationExtensions.cs    HTTP pipeline configuration and endpoint mapping
│   ├── Features/Orders/
│   │   ├── OrderProjections.cs           Shared EF Core projections
│   │   ├── PagedResult.cs                Generic paged response wrapper
│   │   ├── PaginationQuery.cs            [AsParameters] page/pageSize binding
│   │   ├── CreateOrder/                  POST   /api/v1/orders
│   │   ├── DeleteOrder/                  DELETE /api/v1/orders/{id}
│   │   ├── GetDeletedOrders/             GET    /api/v1/orders/deleted
│   │   ├── GetOrderById/                 GET    /api/v1/orders/{id}
│   │   ├── GetOrderHistory/              GET    /api/v1/orders/{id}/history
│   │   ├── GetOrders/                    GET    /api/v1/orders
│   │   ├── GetOrdersByStatus/            GET    /api/v1/orders/status/{name}
│   │   ├── GetProfitByMonth/             GET    /api/v1/orders/profit/monthly
│   │   ├── SearchOrders/                 GET    /api/v1/orders/search
│   │   └── UpdateOrderStatus/            PATCH  /api/v1/orders/{id}/status
│   ├── Infrastructure/
│   │   ├── Entities/                     EF Core entity classes (Order, OrderItem, …)
│   │   ├── EntityConfigurations/         Fluent API entity configurations
│   │   ├── Health/
│   │   │   ├── LivenessHealthEndpoint.cs GET /health/live — always 200 Healthy
│   │   │   └── ReadinessHealthEndpoint.cs GET /health/ready — DB check, 503 on failure
│   │   ├── Migrations/                   EF Core schema migrations
│   │   ├── OrderContext.cs               DbContext with soft-delete query filters
│   │   └── OrderContextFactory.cs        Design-time factory for migrations CLI
│   ├── Middleware/
│   │   ├── CorrelationIdMiddleware.cs    X-Correlation-ID propagation
│   │   └── IdempotencyMiddleware.cs      POST deduplication via Idempotency-Key
│   ├── Services/
│   │   ├── IdempotencyCleanupService.cs  IHostedService — periodic idempotency record expiry
│   │   └── StaleOrderCleanupService.cs  IHostedService — periodic stale order cancellation
│   ├── Program.cs
│   └── appsettings.json
└── OrderApi.Tests/
    ├── ArchitectureTests.cs              ArchUnitNET naming + coupling rules
    ├── Common/
    │   └── OrderApiTestFactory.cs        WebApplicationFactory + SQLite
    └── Features/
        ├── Background/
        │   └── StaleOrderCleanupServiceTests.cs
        └── Orders/
            ├── OrdersEndpointTests.cs                    Shared fixture setup (partial class)
            ├── OrdersEndpointTests.GetOrdersTests.cs
            ├── OrdersEndpointTests.GetOrderByIdTests.cs
            ├── OrdersEndpointTests.GetOrdersByStatusTests.cs
            ├── OrdersEndpointTests.ProfitTests.cs
            ├── OrdersEndpointTests.HealthTests.cs
            ├── OrdersEndpointTests.WriteTests.cs
            ├── OrdersEndpointTests.MiddlewareTests.cs
            ├── OrdersEndpointTests.SoftDeleteHistorySearchTests.cs
            ├── OrdersEndpointTests.ConcurrencyIdempotencyTests.cs
            ├── CreateOrderValidatorTests.cs
            └── UpdateOrderStatusValidatorTests.cs
```

Each feature slice contains its own `README.md` with the full HTTP contract and implementation notes co-located alongside the slice code (e.g. `Features/Orders/CreateOrder/README.md`).

### v2 Technology Stack

| Concern | Choice | Version |
|---|---|---|
| Runtime | .NET | 10.0 |
| Web framework | ASP.NET Core Minimal API | 10.0 |
| CQRS / Mediator | MediatR | 12.x |
| ORM | Pomelo EF Core MySQL | 9.0 |
| Validation | FluentValidation | 11.x |
| API docs | Native OpenAPI + Scalar UI | 10.x / 2.x |
| Structured logging | Serilog | 4.x |
| Observability | OpenTelemetry (traces + metrics) | 1.x |
| Output caching | Redis + ASP.NET Core OutputCache | 7.x |
| API versioning | Asp.Versioning.Http | 8.x |
| Health checks | Built-in ASP.NET Core health checks | – |
| Exception handling | `IExceptionHandler` (GlobalExceptionHandler) | – |
| Background services | `IHostedService` (StaleOrderCleanupService + IdempotencyCleanupService) | – |
| Architecture tests | ArchUnitNET | – |
| Test framework | xUnit + WebApplicationFactory + SQLite | 2.x |
| Database | MySQL | 5.7 (Docker) |
| Cache | Redis | 7.x (Docker) |

### v2 Getting Started

#### Run with Docker (v2 only)

```bash
# From the repo root — copy .env.example to .env and set DB_PASSWORD first
docker compose -f docker-compose.v2.yml up
```

The v2 API runs on **http://localhost:8001**  
Scalar UI: **http://localhost:8001/scalar/v1** (development mode)  
Liveness: **http://localhost:8001/health/live**  
Readiness: **http://localhost:8001/health/ready**

#### Run locally

```bash
cd src-v2
dotnet run --project OrderApi
```

#### Run v2 tests

```bash
cd src-v2
dotnet test
```

83 tests — integration tests split by feature, unit validator tests, architecture tests, background job tests. No Docker required.

### v2 API Endpoints

All routes are prefixed with `/api/v1/`. Two rate-limiting tiers apply: **`fixed`** (100 req/min) and **`expensive`** (10 req/min).

| Method | Route | Description | Cache | Rate limit |
|---|---|---|---|---|
| `GET` | `/api/v1/orders` | Paginated list of all orders | ✅ 1 min | fixed |
| `GET` | `/api/v1/orders/{orderId}` | Order detail + ETag response header | ✅ 1 min | fixed |
| `GET` | `/api/v1/orders/status/{statusName}` | Orders filtered by status | ✅ 1 min | fixed |
| `GET` | `/api/v1/orders/search` | Multi-filter search with pagination | ✅ 1 min | expensive |
| `GET` | `/api/v1/orders/profit/monthly` | Monthly profit for Completed orders | ✅ 1 min | expensive |
| `GET` | `/api/v1/orders/{orderId}/history` | Status change audit log | ❌ | fixed |
| `GET` | `/api/v1/orders/deleted` | Paginated soft-deleted orders | ❌ | fixed |
| `POST` | `/api/v1/orders` | Create a new order (idempotent via `Idempotency-Key`) | evicts | fixed |
| `PATCH` | `/api/v1/orders/{orderId}/status` | Update status (requires `If-Match` ETag) | evicts | fixed |
| `DELETE` | `/api/v1/orders/{orderId}` | Soft-delete an order | evicts | fixed |
| `GET` | `/health/live` | Liveness probe — always 200 | ❌ | – |
| `GET` | `/health/ready` | Readiness probe — checks DB + Redis | ❌ | – |

### v1 vs v2 Comparison

| Feature | v1 (src/) | v2 (src-v2/) |
|---|---|---|
| Architecture | Layered (Controller → Service → Repository) | Vertical Slice (each feature is self-contained) |
| API style | ASP.NET Core MVC Controllers | Minimal API |
| .NET version | 8.0 LTS | 10.0 |
| CQRS | Direct service calls | MediatR with pipeline behaviours |
| API docs | Swashbuckle (Swagger UI) | Native OpenAPI + Scalar UI |
| Structured logging | `Microsoft.Extensions.Logging` | Serilog (enriched, structured) |
| Observability | None | OpenTelemetry (W3C traces + metrics) |
| Output caching | None | Redis (prod) / in-memory (dev) |
| API versioning | None | `/api/v1/` via `Asp.Versioning.Http` |
| Schema management | SQL init script (`mysql-init.sql`) | EF Core migrations (auto-applied) |
| Idempotency | None | `Idempotency-Key` header deduplication |
| Concurrency control | ConcurrencyStamp (reverts on conflict) | ETag + `If-Match` header |
| Soft delete | None | `IsDeleted` global query filter |
| Order history | None | `OrderStatusHistory` audit log |
| Background jobs | None | StaleOrderCleanupService (hourly) |
| Domain events | None | `IOrderEventPublisher` (Null impl / outbox hook) |
| Search | Status filter only | Full search — 7 filters + pagination |
| Rate limiting | Per-user fixed window (100/min) | Dual-tier (`fixed` 100/min, `expensive` 10/min) |
| Health probes | `/health/live` + `/health/ready` | `/health/live` + `/health/ready` |
| Exception handling | `IExceptionHandler` middleware | `IExceptionHandler` interface |
| Test runner | NUnit 4 | xUnit 2 |
| Architecture tests | None | ArchUnitNET (naming + coupling) |
| Test count | 65 (37 service + 28 API) | 83 (per-feature split) |

---

## Improvements & Design Decisions

Both versions started as a basic tech-test skeleton and were substantially enhanced to demonstrate production-grade patterns. This section documents what was added and why, grouped by concern.

### Security

| Improvement | Version | Rationale |
|---|---|---|
| **Conditional JWT auth** | V1 + V2 | `Jwt:Authority` is optional — empty means no enforcement (local dev). Set it via env var for production. Without this guard, every `dotnet run` would require a running IdP. |
| **Scope-based authorization policies** | V2 | `orders:read` / `orders:write` JWT scope claims give fine-grained access control. A read-only token cannot mutate orders. |
| **`[Authorize]` on controller** | V1 | Without this, every endpoint was publicly accessible. Auth should be opt-out, not opt-in. |
| **Configurable CORS** | V1 + V2 | `AllowAnyOrigin` is a security risk in production. The API reads `Cors:AllowedOrigins` from config; AllowAny is only permitted when the environment is Development. |
| **Connection string via env / `.env`** | V1 + V2 | Removed plaintext credentials from `appsettings.Development.json`. Passwords must not be in source control under any circumstances. |

### Performance

| Improvement | Version | Rationale |
|---|---|---|
| **Server-side SQL aggregations** | V1 + V2 | `GetMonthlyProfitAsync` originally loaded all completed orders into memory. Rewritten to use `GROUP BY` in SQL — scales to millions of rows without OOM risk. |
| **ResellerId DB index** | V1 | Filtering by ResellerId is the primary query pattern. Without an index, every filter is a full table scan. |
| **Output caching (Redis + in-memory)** | V2 | Read-heavy list endpoints are cached for 1 minute with tag-based eviction on mutations. Redis ensures cache is shared across replicas — no stale data between pods. |
| **Response compression (Brotli + Gzip)** | V1 + V2 | Significantly reduces bandwidth for large order list payloads, especially over slow connections. |
| **Server-side pagination** | V2 | `SearchOrders` and all list endpoints use `page`/`pageSize` — never returns unbounded result sets. |

### Resilience

| Improvement | Version | Rationale |
|---|---|---|
| **Polly retry on EF Core** | V1 + V2 | `EnableRetryOnFailure(3, 5s)` handles transient MySQL connection failures. A single network blip shouldn't fail a user request permanently. |
| **Idempotency middleware** | V2 | POST deduplication via `Idempotency-Key` header prevents duplicate orders on client retries. The response is stored in the database and replayed — the handler is never re-invoked. |
| **ETag / optimistic concurrency** | V2 | `PATCH` requires an `If-Match` header matching the current `ConcurrencyStamp`. Returns `409 Conflict` on mismatch, preventing last-write-wins data loss. |
| **ConcurrencyStamp revert on conflict (V1)** | V1 | Saves the original stamp before the update, reverts it on `DbUpdateConcurrencyException` to prevent a dirty entity state that would corrupt subsequent requests. |
| **Soft delete** | V2 | Orders are never hard-deleted. `IsDeleted` flag with EF Core global query filter (cascades to `OrderItem`). Financial records should always be recoverable. |
| **Background stale order cleanup** | V2 | Orders stuck in `Created` status for 48 hours are automatically cancelled by an `IHostedService`. Prevents orphaned orders from polluting active data without manual DBA intervention. |
| **Global exception handler** | V1 + V2 | Unhandled exceptions return a consistent JSON `ProblemDetails` response with no stack trace leak. |

### Observability

| Improvement | Version | Rationale |
|---|---|---|
| **Correlation ID middleware** | V1 + V2 | `X-Correlation-ID` is read from the request header (or generated if absent) and echoed in the response. Enriches log scope so every log line for a request shares the same correlation ID — essential for tracing across microservices. |
| **Serilog structured logging** | V2 | Replaces `Microsoft.Extensions.Logging`. Every entry is enriched with `ServiceName`, `Environment`, and `CorrelationId`. Structured JSON logs are consumable by Elastic/Splunk/Application Insights without parsing. |
| **OpenTelemetry** | V2 | W3C `traceparent` propagation. Spans for HTTP requests and EF Core queries. `/health` endpoints are filtered from traces to reduce noise. Console exporter in dev; swap for OTLP/Azure Monitor in production. |
| **MediatR LoggingBehavior** | V2 | Every command and query is automatically logged (request type + duration) by the pipeline — no feature code is modified to get this. |
| **Split health probes** | V1 + V2 | `/health/live` (no checks — pod is alive) vs `/health/ready` (DB + Redis check — pod can serve traffic). Kubernetes treats these differently: liveness failure restarts the pod; readiness failure removes it from the load balancer. |
| **Redis health check** | V2 | Conditional on `Redis:ConnectionString`. A Redis outage fails the readiness probe, removing the pod from the load balancer until caching is restored. |

### API Design

| Improvement | Version | Rationale |
|---|---|---|
| **API versioning (`/api/v1/`)** | V2 | `Asp.Versioning.Http` routes all endpoints under `/api/v1/`. Adding a breaking `/api/v2/` doesn't break existing clients. |
| **SearchOrders with filters** | V2 | Single endpoint with 7 optional filters (date range, reseller, customer, status, total range) plus pagination. Eliminates the need for multiple narrow endpoints and is far more flexible for client-side querying. |
| **Order status history** | V2 | `OrderStatusHistory` audit log records every status transition with a timestamp. Useful for debugging disputes and compliance. |
| **Soft delete + archive endpoint** | V2 | `DELETE` soft-deletes; `GET /orders/deleted` provides the archive. Meets financial audit requirements. |
| **Domain events** | V2 | `IOrderEventPublisher` exposes a single generic `PublishAsync<TEvent>()`. Three event types are defined: `OrderCreatedEvent`, `OrderStatusChangedEvent`, `OrderDeletedEvent`. The `NullOrderEventPublisher` is a hook — swap in a real broker (Azure Service Bus, Kafka) using the outbox pattern to avoid dual-write issues. |

### Architecture & Developer Experience

| Improvement | Version | Rationale |
|---|---|---|
| **Vertical slice architecture** | V2 | Each feature slice (`Features/Orders/{Name}/`) owns its handler, validator, request, and response. No cross-feature coupling. Adding a feature means creating a new folder — nothing else changes. |
| **MediatR pipeline (ValidationBehavior)** | V2 | All commands and queries are validated automatically before the handler runs. No `if (!valid) return BadRequest()` in feature code. |
| **EF Core migrations** | V2 | Schema changes are version-controlled and auto-applied on startup. No manual SQL scripts, no schema drift between environments. |
| **Architecture tests (ArchUnitNET)** | V2 | Enforces naming conventions and prevents cross-feature coupling at CI time. The rules are defined once; they scale as the codebase grows. |
| **Per-feature test files** | V2 | Integration tests are split by concern (`WriteTests`, `SoftDeleteHistorySearchTests`, etc.). Monolithic test files with 200+ tests are hard to navigate and review. |
| **Dual-tier rate limiting** | V1 + V2 | General limit (100/min) protects all endpoints; a stricter limit (10/min) applies to expensive endpoints (`/search`, `/profit/monthly`) that do heavier DB work. Both limits are per-user (partitioned by identity → IP → anonymous) so one client cannot exhaust quota for others. |
