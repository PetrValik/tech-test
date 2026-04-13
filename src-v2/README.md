# Order API v2 — Vertical Slice Architecture

A **.NET 10** Minimal API demonstrating **Vertical Slice Architecture** with MediatR, EF Core migrations, Redis output caching, OpenTelemetry, and a suite of production-grade features. Built as a clean-code reference showing how the same Order domain from [V1](../src/README.md) can be structured very differently — each feature is self-contained and owns everything it needs.

---

## Architecture

### Layered (v1) vs Vertical Slice (v2)

In the **layered** architecture used in v1, every feature cuts across shared horizontal layers:

```
Request → Controller → Service → Repository → Database
```

Each layer owns one concern, but all features share those layers. Adding a new endpoint means touching files across multiple projects.

In the **vertical slice** architecture used here, each feature cuts through the full stack in isolation:

```
Request → Endpoint → Handler → DbContext → Database
```

Each feature slice is self-contained. Adding a new endpoint means adding one folder — nothing else changes.

| | v1 (`src/`) | v2 (`src-v2/`) |
|---|---|---|
| Architecture | Layered (4 projects) | Vertical Slice (1 project) |
| API style | MVC Controllers | Minimal API |
| Routing | `[ApiController]` attributes | `IEndpoint` + assembly scan |
| CQRS | No | MediatR pipeline |
| Validation | Auto model-state | FluentValidation pipeline behaviour |
| Exception handling | Middleware filter | `IExceptionHandler` |
| API docs | Swashbuckle (always-on) | Native OpenAPI + Scalar (dev only) |
| Test runner | NUnit | xUnit |
| Test isolation | Per-test `EnsureCreated` | Per-test `EnsureDeleted` + `EnsureCreated` |
| .NET version | 8.0 LTS | 10.0 |

### Philosophy

Each _feature slice_ lives under `Features/Orders/{FeatureName}/` and owns its own handler, validator, request type, and response type. There is no shared service layer and no repository abstraction — each handler talks directly to the EF Core `OrderContext`. Cross-cutting concerns (validation, logging, auth, rate limiting, caching) live outside feature code.

```
HTTP Request
    └─► IEndpoint.MapEndpoint()  (one class per route, discovered by assembly scan)
                └─► MediatR pipeline
                        ├─► ValidationBehavior   (auto-validates all requests)
                        ├─► LoggingBehavior       (logs request + duration)
                        └─► Handler              (Features/Orders/<Feature>/Handler.cs)
                                    └─► OrderContext  (EF Core → MySQL)
```

### What's notable compared to the classic layered approach

- No controller classes, no service classes, no repository classes
- Adding a new endpoint = creating one new folder; nothing else changes
- MediatR pipeline handles cross-cutting concerns without touching feature code
- EF Core global query filters enforce domain invariants (soft delete) transparently

---

## Project Structure

```
src-v2/
├── OrderApi/
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs       Auto-validates all MediatR requests via FluentValidation
│   │   │   └── LoggingBehavior.cs          Logs request type + duration for every command/query
│   │   ├── Endpoints/
│   │   │   ├── IEndpoint.cs                Contract — each feature endpoint implements MapEndpoint()
│   │   │   └── EndpointExtensions.cs       AddEndpoints() scans assembly; MapEndpoints() wires routes
│   │   └── Events/
│   │       ├── IOrderEventPublisher.cs     Domain event contract (outbox pattern hook)
│   │       ├── NullOrderEventPublisher.cs  Null impl — swap for real broker in production
│   │       ├── OrderCreatedEvent.cs        Event raised when a new order is created
│   │       ├── OrderDeletedEvent.cs        Event raised when an order is soft-deleted
│   │       └── OrderStatusChangedEvent.cs  Event raised when order status changes
│   ├── Exceptions/
│   │   └── GlobalExceptionHandler.cs      IExceptionHandler — consistent JSON ProblemDetails
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs  Focused bootstrap helpers (AddPersistence, etc.)
│   │   └── WebApplicationExtensions.cs    HTTP pipeline configuration and endpoint mapping
│   ├── Features/Orders/
│   │   ├── OrderProjections.cs             Shared EF Core projections (list summaries)
│   │   ├── PagedResult.cs                  Generic paged response wrapper
│   │   ├── PaginationQuery.cs              [AsParameters] page/pageSize binding + validation
│   │   ├── CreateOrder/                    POST   /api/v1/orders
│   │   ├── DeleteOrder/                    DELETE /api/v1/orders/{id}
│   │   ├── GetDeletedOrders/               GET    /api/v1/orders/deleted
│   │   ├── GetOrderById/                   GET    /api/v1/orders/{id}
│   │   ├── GetOrderHistory/                GET    /api/v1/orders/{id}/history
│   │   ├── GetOrders/                      GET    /api/v1/orders
│   │   ├── GetOrdersByStatus/              GET    /api/v1/orders/status/{name}
│   │   ├── GetProfitByMonth/               GET    /api/v1/orders/profit/monthly
│   │   ├── SearchOrders/                   GET    /api/v1/orders/search
│   │   └── UpdateOrderStatus/              PATCH  /api/v1/orders/{id}/status
│   ├── Infrastructure/
│   │   ├── Entities/                       EF Core entity classes (Order, OrderItem, …)
│   │   ├── EntityConfigurations/           Fluent API entity configurations
│   │   ├── Health/
│   │   │   ├── LivenessHealthEndpoint.cs   GET /health/live — always 200 Healthy
│   │   │   └── ReadinessHealthEndpoint.cs  GET /health/ready — DB check, 503 on failure
│   │   ├── Migrations/                     EF Core schema migrations
│   │   ├── OrderContext.cs                 DbContext with soft-delete query filters
│   │   ├── OrderContextFactory.cs          Design-time factory for `dotnet ef` CLI
│   │   └── OrderStatusNames.cs             Shared status name constants (Created, InProgress, …)
│   ├── Middleware/
│   │   ├── CorrelationIdMiddleware.cs      X-Correlation-ID propagation
│   │   └── IdempotencyMiddleware.cs        POST deduplication via Idempotency-Key header
│   ├── Services/
│   │   ├── IdempotencyCleanupService.cs    IHostedService — periodic idempotency record expiry
│   │   └── StaleOrderCleanupService.cs     IHostedService — periodic stale order cancellation
│   ├── Program.cs
│   └── appsettings.json
└── OrderApi.Tests/
    ├── ArchitectureTests.cs                ArchUnitNET naming + coupling rules
    ├── Common/
    │   ├── NoOpOutputCacheStore.cs         No-op IOutputCacheStore; disables caching in tests
    │   ├── OrderApiTestFactory.cs          WebApplicationFactory + SQLite in-memory
    │   ├── OrdersEndpointTestBase.cs       Abstract base: shared client, DB reset, seed helpers
    │   └── TestAuthHandler.cs              Fake auth handler; auto-authenticates every request
    └── Features/
        ├── Background/
        │   └── StaleOrderCleanupServiceTests.cs
        └── Orders/
            ├── OrdersCollection.cs                      xUnit collection definition (shared factory)
            ├── GetOrdersTests.cs
            ├── GetOrderByIdTests.cs
            ├── GetOrdersByStatusTests.cs
            ├── GetProfitByMonthTests.cs
            ├── HealthEndpointTests.cs
            ├── WriteTests.cs
            ├── MiddlewareTests.cs
            ├── SoftDeleteHistorySearchTests.cs
            ├── ConcurrencyIdempotencyTests.cs
            ├── CreateOrderValidatorTests.cs
            └── UpdateOrderStatusValidatorTests.cs
```

Each feature slice contains its own `README.md` co-located with the slice code. See:

| Slice | README |
|---|---|
| CreateOrder | `Features/Orders/CreateOrder/README.md` |
| DeleteOrder | `Features/Orders/DeleteOrder/README.md` |
| GetDeletedOrders | `Features/Orders/GetDeletedOrders/README.md` |
| GetOrderById | `Features/Orders/GetOrderById/README.md` |
| GetOrderHistory | `Features/Orders/GetOrderHistory/README.md` |
| GetOrders | `Features/Orders/GetOrders/README.md` |
| GetOrdersByStatus | `Features/Orders/GetOrdersByStatus/README.md` |
| GetProfitByMonth | `Features/Orders/GetProfitByMonth/README.md` |
| SearchOrders | `Features/Orders/SearchOrders/README.md` |
| UpdateOrderStatus | `Features/Orders/UpdateOrderStatus/README.md` |
| Health | `Infrastructure/Health/README.md` |

---

## Technology Stack

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
| Background services | `IHostedService` (StaleOrderCleanupService + IdempotencyCleanupService) | – |
| Architecture tests | ArchUnitNET | – |
| Test framework | xUnit + WebApplicationFactory + SQLite | 2.x |
| Database | MySQL | 5.7 (Docker) |
| Cache | Redis | 7.x (Docker) |

---

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (Linux containers)

### Run with Docker

```bash
# From the repository root — copy .env.example to .env and set DB_PASSWORD first
cp .env.example .env   # Windows: copy .env.example .env

docker compose -f docker-compose.v2.yml up
```

| URL | Purpose |
|-----|---------|
| http://localhost:8001 | API root |
| http://localhost:8001/scalar/v1 | Interactive API docs (Scalar UI) — dev mode only |
| http://localhost:8001/health/live | Liveness probe |
| http://localhost:8001/health/ready | Readiness probe (DB + Redis) |

### Run Locally

```bash
# Start MySQL and Redis (from repo root)
docker compose -f docker-compose.v2.yml up database redis -d

# Run the API
cd src-v2
dotnet run --project OrderApi
```

EF Core migrations are applied automatically on startup when `Database:ApplyMigrationsOnStartup` is `true` (the default in `Development`).

### Run Tests

No Docker required — tests use SQLite in-memory.

```bash
cd src-v2
dotnet test
```

**83 tests**, 0 failures:

| Test file | What it covers |
|-----------|----------------|
| `OrdersEndpointTests.GetOrdersTests` | GET orders — pagination, empty result |
| `OrdersEndpointTests.GetOrderByIdTests` | GET by ID — found, not found, ETag header |
| `OrdersEndpointTests.GetOrdersByStatusTests` | GET by status — valid/invalid status names |
| `OrdersEndpointTests.ProfitTests` | GET profit/monthly aggregation |
| `OrdersEndpointTests.HealthTests` | Liveness + readiness probes |
| `OrdersEndpointTests.WriteTests` | POST create, PATCH status, DELETE |
| `OrdersEndpointTests.SoftDeleteHistorySearchTests` | Soft delete archive, status history, search filters |
| `OrdersEndpointTests.ConcurrencyIdempotencyTests` | ETag conflict (409), idempotency deduplication |
| `OrdersEndpointTests.MiddlewareTests` | Correlation ID header, rate limiter (429) |
| `CreateOrderValidatorTests` | FluentValidation rules — CreateOrder |
| `UpdateOrderStatusValidatorTests` | FluentValidation rules — UpdateOrderStatus |
| `StaleOrderCleanupServiceTests` | Background job cancels stale Created orders |
| `ArchitectureTests` | ArchUnitNET: naming conventions + no cross-feature deps |

---

## API Endpoints

All order routes are under `/api/v1/orders`. Two rate-limiting tiers apply:
- **`fixed`** — 100 requests/min (all endpoints)
- **`expensive`** — 10 requests/min (`/search` and `/profit/monthly`)

| Method | Route | Description | Cache | Rate limit |
|---|---|---|---|---|
| `GET` | `/api/v1/orders` | Paginated list of all orders | ✅ 1 min | fixed |
| `GET` | `/api/v1/orders/{orderId}` | Order detail + `ETag` response header | ✅ 1 min | fixed |
| `GET` | `/api/v1/orders/status/{statusName}` | Orders filtered by status name | ✅ 1 min | fixed |
| `GET` | `/api/v1/orders/search` | Multi-filter search with pagination | ✅ 1 min | expensive |
| `GET` | `/api/v1/orders/profit/monthly` | Monthly profit for Completed orders | ✅ 1 min | expensive |
| `GET` | `/api/v1/orders/{orderId}/history` | Status change audit log | ❌ | fixed |
| `GET` | `/api/v1/orders/deleted` | Paginated soft-deleted orders | ❌ | fixed |
| `POST` | `/api/v1/orders` | Create a new order (idempotent) | evicts | fixed |
| `PATCH` | `/api/v1/orders/{orderId}/status` | Update status (requires `If-Match` ETag) | evicts | fixed |
| `DELETE` | `/api/v1/orders/{orderId}` | Soft-delete an order | evicts | fixed |
| `GET` | `/health/live` | Liveness probe — always `200 Healthy` | ❌ | – |
| `GET` | `/health/ready` | Readiness probe — checks DB + Redis | ❌ | – |

### Idempotency (POST /orders)

Include an `Idempotency-Key` header (UUID) on `POST /api/v1/orders`. On the first call, the response is stored and the order is created. Any retry with the same key returns the cached response without creating a duplicate.

```bash
curl -X POST http://localhost:8001/api/v1/orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000" \
  -d '{
    "resellerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "items": [
      { "productId": "existing-product-guid", "quantity": 5 }
    ]
  }'
```

**Response:** `201 Created`

```json
{ "id": "new-order-guid" }
```

**Failure (invalid product IDs):** `400 Bad Request`

```json
{
  "error": "One or more product IDs are invalid.",
  "invalidProductIds": ["non-existent-guid"]
}
```

### GET /api/v1/orders

```bash
curl "http://localhost:8001/api/v1/orders?page=1&pageSize=20"
```

**Response:** `200 OK`

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "resellerId": "aabbccdd-0000-0000-0000-000000000001",
      "customerId": "aabbccdd-0000-0000-0000-000000000002",
      "statusId": "aabbccdd-0000-0000-0000-000000000010",
      "statusName": "Created",
      "createdDate": "2025-03-01T10:00:00Z",
      "itemCount": 2,
      "totalCost": 80.00,
      "totalPrice": 100.00
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

### GET /api/v1/orders/{orderId}

```bash
curl -i "http://localhost:8001/api/v1/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6"
# Response headers include: ETag: "abc123-concurrency-stamp"
```

**Response:** `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "resellerId": "aabbccdd-0000-0000-0000-000000000001",
  "customerId": "aabbccdd-0000-0000-0000-000000000002",
  "statusId": "aabbccdd-0000-0000-0000-000000000010",
  "statusName": "Created",
  "createdDate": "2025-03-01T10:00:00Z",
  "totalCost": 80.00,
  "totalPrice": 100.00,
  "items": [
    {
      "id": "item-guid-1",
      "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "serviceId": "service-guid",
      "serviceName": "Email",
      "productId": "product-guid",
      "productName": "100GB Mailbox",
      "quantity": 5,
      "unitCost": 8.00,
      "unitPrice": 10.00,
      "totalCost": 40.00,
      "totalPrice": 50.00
    }
  ],
  "concurrencyStamp": "abc123-concurrency-stamp"
}
```

Returns `404 Not Found` if the order does not exist or has been soft-deleted.

### GET /api/v1/orders/status/{statusName}

```bash
curl "http://localhost:8001/api/v1/orders/status/Completed?page=1&pageSize=50"
```

**Response:** `200 OK` — same `PagedResult<OrderSummaryResponse>` shape as `GET /api/v1/orders`.

Valid status names: `Created`, `In Progress`, `Failed`, `Completed`.

### GET /api/v1/orders/profit/monthly

```bash
curl "http://localhost:8001/api/v1/orders/profit/monthly"
```

**Response:** `200 OK`

```json
[
  { "year": 2025, "month": 1, "totalProfit": 150.00 },
  { "year": 2025, "month": 2, "totalProfit": 230.50 }
]
```

Returns data for the last 24 months of Completed orders.

### GET /api/v1/orders/{orderId}/history

```bash
curl "http://localhost:8001/api/v1/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6/history"
```

**Response:** `200 OK`

```json
{
  "items": [
    {
      "fromStatus": "Created",
      "toStatus": "In Progress",
      "changedAt": "2025-03-02T09:00:00Z"
    },
    {
      "fromStatus": "In Progress",
      "toStatus": "Completed",
      "changedAt": "2025-03-03T14:30:00Z"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

### GET /api/v1/orders/deleted

```bash
curl "http://localhost:8001/api/v1/orders/deleted?page=1&pageSize=20"
```

**Response:** `200 OK` — same `PagedResult<OrderSummaryResponse>` shape as `GET /api/v1/orders`, containing only soft-deleted orders.

### GET /api/v1/orders/search

All parameters are optional. Omitting all filters returns the same result as `GET /api/v1/orders`.

```bash
curl "http://localhost:8001/api/v1/orders/search?status=Completed&from=2025-01-01&minTotal=100&page=1&pageSize=25"
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `from` | `DateTime?` | Orders created on or after this UTC date |
| `to` | `DateTime?` | Orders created on or before this UTC date |
| `resellerId` | `Guid?` | Filter by reseller |
| `customerId` | `Guid?` | Filter by customer |
| `status` | `string?` | Filter by status name |
| `minTotal` | `decimal?` | Minimum `TotalPrice` (inclusive) |
| `maxTotal` | `decimal?` | Maximum `TotalPrice` (inclusive) |
| `page` | `int` | Page number (default 1) |
| `pageSize` | `int` | Page size (default 50, max 200) |

**Response:** `200 OK` — same `PagedResult<OrderSummaryResponse>` shape.

### PATCH /api/v1/orders/{orderId}/status — Optimistic Concurrency

`GET /api/v1/orders/{id}` returns an `ETag` header containing the current `ConcurrencyStamp`. Include it as `If-Match` when patching status. If the order was modified by another client in the meantime, the server returns `409 Conflict`.

```bash
# 1. Get the current ETag
curl -I http://localhost:8001/api/v1/orders/{id}
# ETag: "abc123"

# 2. Update with If-Match
curl -X PATCH http://localhost:8001/api/v1/orders/{id}/status \
  -H "Content-Type: application/json" \
  -H "If-Match: \"abc123\"" \
  -d '{ "statusName": "Completed" }'
# 204 No Content on success, 409 Conflict if someone else updated first
```

| HTTP Status | Meaning |
|-------------|---------|
| `204` | Status updated successfully |
| `400` | `statusName` is not a valid lifecycle value |
| `404` | Order not found |
| `409` | ETag mismatch — order was modified by another request |

### DELETE /api/v1/orders/{orderId}

Soft-deletes the order. The record is retained in the database and accessible via `GET /api/v1/orders/deleted`.

```bash
curl -X DELETE http://localhost:8001/api/v1/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6
# 204 No Content on success, 404 if not found
```

---

## Key Design Decisions

### 1. Vertical Slice Architecture

Features live under `Features/Orders/{FeatureName}/`. Each slice contains its handler, validator, request, and response types. There is no shared service layer — if two features need similar data, they each have their own query. Duplication within a slice is accepted; coupling across slices is not. Adding a new endpoint means creating a new folder with no impact on existing code.

### 2. MediatR Pipeline

All commands and queries flow through:

```
Request → ValidationBehavior → LoggingBehavior → Handler
```

- **`ValidationBehavior`** runs FluentValidation automatically. If validation fails, the handler is never invoked and a `400` with the validation errors is returned. Feature code contains zero validation boilerplate.
- **`LoggingBehavior`** logs the request type and duration at `Information` level. No feature code is modified to get this behaviour.

### 3. EF Core Migrations

Schema changes are version-controlled as EF Core migration files under `Infrastructure/Migrations/`. Migrations are applied automatically on startup in Development and Docker (`Database:ApplyMigrationsOnStartup=true`). In production CI/CD, run `dotnet ef database update` as a deployment step instead of relying on auto-apply.

### 4. Idempotency Middleware

`IdempotencyMiddleware` intercepts all `POST` requests that include an `Idempotency-Key` header. The response body and status code are stored in the database (`IdempotencyRecord` entity) keyed by the idempotency key. On retry, the stored response is written to the HTTP response directly — the MediatR pipeline and handler are never invoked. This prevents duplicate order creation on network retries without any changes to feature code.

### 5. ETag / Optimistic Concurrency

`Order` has a `ConcurrencyStamp` column (GUID, regenerated on every write by the handler). `GET /orders/{id}` includes the stamp as an `ETag` response header. `PATCH /orders/{id}/status` reads the `If-Match` request header and passes it to the handler, which checks it against the current stamp before saving. A mismatch returns `409 Conflict`. No last-write-wins data loss is possible.

### 6. Soft Delete

`Order` has an `IsDeleted` flag. `DELETE /orders/{id}` sets it to `true`. An EF Core global query filter on `OrderContext` automatically excludes soft-deleted records from every query that goes through the context (cascades to `OrderItem` via navigation properties). `GET /orders/deleted` uses `IgnoreQueryFilters()` to expose the archive. Financial orders should never be hard-deleted.

### 7. SearchOrders

`GET /orders/search` accepts up to 7 optional filter parameters: `from`, `to`, `resellerId`, `customerId`, `status`, `minTotal`, `maxTotal` — plus `page`/`pageSize`. All filtering and pagination happen in SQL on the server. No full result sets are loaded into application memory.

### 8. Output Caching

Read endpoints (list, detail, status filter, search, profit) use a 1-minute output cache tagged `"orders"`. When any mutation succeeds (create, update, delete), the tag is evicted via `IOutputCacheStore.EvictByTagAsync`. In production (with `Redis:ConnectionString` set), the cache is shared across all API replicas. Without Redis, it falls back silently to per-instance in-memory caching.

### 9. Background Stale Order Cleanup

`StaleOrderCleanupService` (an `IHostedService`) runs every hour. It finds orders in `Created` status that were created more than 30 days ago and sets their status to `Failed`. This prevents orphaned orders from polluting active data without any manual DBA intervention.

### 10. OpenTelemetry

W3C `traceparent` propagation is enabled. Spans are created for every HTTP request and EF Core database query. Health check endpoints are filtered out of traces to reduce noise. The console exporter is active in development and staging. To export to a real backend, replace it in `ServiceCollectionExtensions.cs`:
- Azure Monitor: `.AddAzureMonitorTraceExporter(o => o.ConnectionString = ...)`
- Jaeger / OTLP: `.AddOtlpExporter(o => o.Endpoint = new Uri(...))`

### 11. API Versioning

Routes are under `/api/v1/`. `Asp.Versioning.Http` reads the version from the URL segment and from the `X-Api-Version` request header. When a breaking change is required, introduce handlers under `/api/v2/` alongside the existing ones — existing `/api/v1/` clients are unaffected.

### 12. Split Health Probes

- **`/health/live`** — no health checks registered; always returns `200 Healthy`. Used by Kubernetes liveness probe. If this fails, the pod is restarted.
- **`/health/ready`** — runs the DB context check and, when configured, the Redis check. Returns `503` if any check fails. Used by Kubernetes readiness probe to remove the pod from the load balancer until its dependencies are available.

### 13. Domain Events

`IOrderEventPublisher` exposes a single generic `PublishAsync<TEvent>()` method. Three event types are defined: `OrderCreatedEvent`, `OrderDeletedEvent`, and `OrderStatusChangedEvent`. `NullOrderEventPublisher` is the current implementation — it logs the event and does nothing. In production, inject a real implementation that publishes to Azure Service Bus, AWS SQS, or Kafka. Use the outbox pattern (transactional outbox) to avoid the dual-write problem: write the event to the same database transaction as the order change, then have a relay publish it to the broker.

### 14. JWT Bearer / Scope Policies

JWT Bearer authentication is always registered. `Jwt:Authority` and `Jwt:Audience` are read from configuration (env vars in Docker). Two scope-based authorization policies are defined:

- **`ReadOrders`** — requires `orders:read` scope claim
- **`WriteOrders`** — requires `orders:write` scope claim

When `Jwt:Authority` is empty (local dev), the fallback policy is `null` — all requests are permitted. Set `Jwt:Authority` to your IdP's issuer URL to enforce authentication. No code changes required.

### 15. Server-Side Aggregations

`TotalCost` and `TotalPrice` on order summaries are computed in the `SELECT` projection using `Sum()`. The monthly profit query uses a `GroupBy` translated to `GROUP BY` SQL. No order items are loaded into application memory for aggregation.

### 16. Architecture Tests

`ArchitectureTests.cs` (ArchUnitNET) enforces:
- Feature handlers are `internal` (not exposed outside their assembly)
- No cross-feature dependencies (a feature slice must not reference another feature slice's namespace)
- Naming conventions (classes with `Handler` suffix implement `IRequestHandler`)

These rules are checked in CI and prevent accidental coupling as the codebase grows.

### 17. Dual-Tier Rate Limiting

Two fixed-window policies:
- **`fixed`** (100 req/min) — applied individually to each endpoint
- **`expensive`** (10 req/min) — applied additionally to `/search` and `/profit/monthly`

Both policies are partitioned by authenticated user identity → client IP → `"anonymous"`, so one client cannot exhaust the quota for others.

### 18. Serilog Structured Logging

Serilog replaces `Microsoft.Extensions.Logging`. Every log entry is enriched with `ServiceName`, `Environment`, and `CorrelationId` (set by `CorrelationIdMiddleware`). `UseSerilogRequestLogging` adds `RequestHost` and `UserAgent` to each HTTP request log entry. The output is structured JSON — consumable by Elastic/Splunk/Application Insights without log parsing. In production, configure a Serilog sink (e.g., `Serilog.Sinks.ApplicationInsights`) to ship logs to your aggregator.

---

## Authentication

The API uses **JWT Bearer** authentication. Two scope-based authorization policies are defined:

| Policy | Required JWT scope claim | Applied to |
|--------|--------------------------|-----------|
| `ReadOrders` | `orders:read` | All `GET` endpoints |
| `WriteOrders` | `orders:write` | `POST`, `PATCH`, `DELETE` endpoints |

Configuration is read from:

| Key | Description |
|-----|-------------|
| `Jwt:Authority` | Identity provider issuer URL (e.g. Azure AD, Keycloak, Cognito). |
| `Jwt:Audience` | Expected `aud` claim in the token (default: `orders-api`). |

**Conditional bypass for local development:** When `Jwt:Authority` is empty, the fallback authorization policy is set to `null` — all requests are permitted. Set `Jwt:Authority` via environment variable to enforce authentication; no code change is required.

```bash
# .env or docker-compose environment:
Jwt__Authority=https://login.microsoftonline.com/{tenant}/v2.0
Jwt__Audience=orders-api
```

## Pagination

All list endpoints accept `page` and `pageSize` query parameters, bound via `[AsParameters] PaginationQuery`:

| Parameter | Default | Constraints | Description |
|-----------|---------|-------------|-------------|
| `page` | `1` | 1 – 1,000,000 | 1-based page number |
| `pageSize` | `50` | 1 – 200 | Items per page |

Out-of-range values return `400 Bad Request`.

**Response envelope** (`PagedResult<T>`):

```json
{
  "items": [ /* array of T */ ],
  "totalCount": 250,
  "page": 2,
  "pageSize": 50,
  "totalPages": 5
}
```

`totalPages` is computed as `⌈totalCount / pageSize⌉`.

## Configuration Reference

All keys are read from `appsettings.json` and can be overridden via environment variables using `__` as the key separator (e.g. `Database__ApplyMigrationsOnStartup=true`).

| Key | Default | Description |
|-----|---------|-------------|
| `OrderConnectionString` | _(placeholder)_ | MySQL connection string. **Always override via env var in non-dev environments.** |
| `MySqlVersion` | `5.7.0-mysql` | MySQL server version hint for Pomelo EF Core. |
| `Database:ApplyMigrationsOnStartup` | `false` | Auto-applies EF Core migrations on startup. Set `true` in Docker/Docker Compose. |
| `Jwt:Authority` | `""` | IdP issuer URL. Empty = no auth enforcement (local dev). |
| `Jwt:Audience` | `orders-api` | Expected JWT audience claim. |
| `Redis:ConnectionString` | `""` | Redis connection string for output cache. Empty = in-memory fallback. |
| `Otel:ServiceName` | `order-api-v2` | OpenTelemetry service name (appears in traces and logs). |
| `Otel:Environment` | `Development` | Environment tag enriched into every log and trace span. |
| `StaleOrderCleanup:IntervalMinutes` | `60` | How often the background cleanup job runs (minutes). |
| `StaleOrderCleanup:StaleDays` | `30` | Orders in `Created` status older than this many days are set to `Failed`. |
| `Serilog:MinimumLevel:Default` | `Information` | Minimum log level. |
| `AllowedHosts` | `*` | Host filtering. Restrict in production. |

## Validation Rules

### CreateOrder

| Field | Rule |
|-------|------|
| `ResellerId` | Required, non-empty GUID |
| `CustomerId` | Required, non-empty GUID |
| `Items` | Required, 1–100 items, no nulls, no duplicate ProductIds |
| `Items[].ProductId` | Required, non-empty GUID, must exist in the database |
| `Items[].Quantity` | 1 – 1,000,000 |

### UpdateOrderStatus

| Field | Rule |
|-------|------|
| `StatusName` | Required; must be one of: `Created`, `In Progress`, `Failed`, `Completed` |
