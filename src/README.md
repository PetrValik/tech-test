# Order API v1 — Enterprise Layered Architecture

A .NET 8 MVC Web API demonstrating a traditional **layered architecture** (Controller → Service → Repository → Data) for an order management system. Designed as a proof-of-concept reference for developers familiar with enterprise .NET patterns.

## Quick Start

### Docker (MySQL + API)

Run from the **repository root**:

```bash
# Copy .env.example to .env and set DB_PASSWORD first
cp .env.example .env   # Windows: copy .env.example .env

docker compose -f docker-compose.v1.yml up --build
```

The API starts at `http://localhost:8000`. Swagger UI available at `/swagger` in development mode.

### Local Development

```bash
# Ensure MySQL is running and set the connection string
# Linux/macOS:
export OrderConnectionString="server=localhost;port=3306;database=orders;user=order-service;password=yourpassword"
# Windows:
set OrderConnectionString=server=localhost;port=3306;database=orders;user=order-service;password=yourpassword

cd src/Order.WebAPI
dotnet run
```

### Run Tests

```bash
cd src
dotnet test Order.sln
```

Tests use in-memory SQLite — no Docker or MySQL needed.

## Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/orders` | ✅ | List all orders (paged summaries) |
| `GET` | `/orders/{orderId}` | ✅ | Order detail with line items |
| `GET` | `/orders/status/{statusName}` | ✅ | Orders filtered by status |
| `GET` | `/orders/profit/monthly` | ✅ | Monthly profit for completed orders |
| `POST` | `/orders` | ✅ | Create a new order |
| `PATCH` | `/orders/{orderId}/status` | ✅ | Update order status |
| `GET` | `/health/live` | ❌ | Liveness probe — always 200, no dependency checks |
| `GET` | `/health/ready` | ❌ | Readiness probe — checks DB connectivity |

> **Auth** — JWT Bearer is enforced when `Jwt:Authority` is configured (see [Authentication](#authentication)). In local development without `Jwt:Authority` set, all requests are permitted.

## Example Requests

### GET /orders

```bash
curl "http://localhost:8000/orders?page=1&pageSize=20"
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
  "pageSize": 20
}
```

### GET /orders/{orderId}

```bash
curl "http://localhost:8000/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6"
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
  ]
}
```

Returns `404 Not Found` if the order does not exist.

### POST /orders — Create Order

```bash
curl -X POST http://localhost:8000/orders \
  -H "Content-Type: application/json" \
  -d '{
    "resellerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "items": [
      { "productId": "existing-product-guid", "quantity": 5 }
    ]
  }'
```

**Response:** `201 Created` with `Location` header pointing to the new order.

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

### PATCH /orders/{orderId}/status — Update Status

```bash
curl -X PATCH http://localhost:8000/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6/status \
  -H "Content-Type: application/json" \
  -d '{ "statusName": "Completed" }'
```

**Response:** `204 No Content` on success.

Valid status names: `Created`, `In Progress`, `Failed`, `Completed` (case-sensitive).

| HTTP Status | Meaning |
|-------------|---------|
| `204` | Status updated successfully |
| `400` | `statusName` is not a valid lifecycle value |
| `404` | Order not found |
| `409` | Concurrent modification detected — retry |

### GET /orders/status/{statusName}

```bash
curl "http://localhost:8000/orders/status/Completed?page=1&pageSize=50"
```

**Response:** `200 OK` — same `PagedResult<OrderSummary>` shape as `GET /orders`.

### GET /orders/profit/monthly

```bash
curl http://localhost:8000/orders/profit/monthly
```

**Response:** `200 OK`

```json
[
  { "year": 2025, "month": 1, "totalProfit": 150.00 },
  { "year": 2025, "month": 2, "totalProfit": 230.50 }
]
```

## Authentication

The API uses **JWT Bearer** authentication via ASP.NET Core's built-in scheme.

All controller actions are decorated with `[Authorize]`. The authentication middleware is registered in `Startup.cs` and reads two configuration keys:

| Key | Purpose |
|-----|---------|
| `Jwt:Authority` | Issuer URL of your identity provider (e.g. Azure AD, Keycloak). |
| `Jwt:Audience` | Expected `aud` claim in the token (default: `orders-api`). |

**Conditional bypass for local development:** When `Jwt:Authority` is empty, the fallback authorization policy is set to `null` — all requests are permitted without a token. Set `Jwt:Authority` (via environment variable or `.env`) to enforce authentication. No code change is required; one environment variable switches auth on.

```bash
# Docker / production — set JWT_AUTHORITY in .env or as environment variable:
Jwt__Authority=https://login.microsoftonline.com/{tenant}/v2.0
Jwt__Audience=orders-api
```

## Pagination

All list endpoints (`GET /orders` and `GET /orders/status/{name}`) accept two optional query-string parameters:

| Parameter | Default | Max | Description |
|-----------|---------|-----|-------------|
| `page` | `1` | — | 1-based page number |
| `pageSize` | `50` | `200` | Items per page (clamped server-side to 200) |

**Response envelope** (`PagedResult<T>`):

```json
{
  "items": [ /* array of T */ ],
  "totalCount": 250,
  "page": 2,
  "pageSize": 50
}
```

To iterate all pages, increment `page` until `(page - 1) * pageSize >= totalCount`.

## Configuration Reference

All configuration keys read from `appsettings.json` (overridable by environment variables using `__` as separator):

| Key | Default | Description |
|-----|---------|-------------|
| `OrderConnectionString` | _(placeholder)_ | MySQL connection string. **Always override via env var in non-dev environments.** |
| `Jwt:Authority` | `""` | Identity provider issuer URL. Empty = no auth enforcement. |
| `Jwt:Audience` | `orders-api` | Expected JWT audience claim. |
| `Cors:AllowedOrigins` | `[]` | Allowed CORS origins. Empty in Development = any origin. Must be explicitly set for production. |
| `Logging:LogLevel:Default` | `Information` | Default log level. |
| `Logging:LogLevel:Microsoft` | `Warning` | ASP.NET Core framework log level. |
| `AllowedHosts` | `*` | Host filtering. Restrict in production. |

## Data Model

```
OrderService (1) ──► (N) OrderProduct (1) ──► (N) OrderItem (N) ◄── (1) Order
                                                                          │
                                                                     OrderStatus
```

- **Order** — Reseller, customer, status, creation date
- **OrderItem** — Links order to product with quantity
- **OrderProduct** — Name, unit cost, unit price, belongs to a service
- **OrderService** — Top-level category (e.g., Email, Antivirus)
- **OrderStatus** — Lifecycle state: Created → In Progress → Completed/Failed

## Validation Rules

| Field | Rule |
|-------|------|
| `ResellerId` | Required, non-empty GUID |
| `CustomerId` | Required, non-empty GUID |
| `Items` | Required, 1–100 items, no nulls, no duplicate ProductIds |
| `Items[].ProductId` | Required, non-empty GUID, must exist in database |
| `Items[].Quantity` | 1 – 1,000,000 |
| `StatusName` (update) | Required, must be one of: Created, In Progress, Failed, Completed |

## Test Coverage

**65 tests** (37 service + 28 integration), 0 failures.

| Category | Count | Description |
|----------|-------|-------------|
| Service unit tests | 37 | All OrderService business logic via SQLite |
| GET endpoints | 6 | All orders, by ID, by status, empty lists |
| POST /orders | 9 | Valid, null body, empty items, quantity bounds, duplicates, roundtrip, too many items, multi-product |
| PATCH status | 4 | Valid update, not found, null body, invalid status |
| Profit | 2 | Monthly grouping, empty result |
| Health | 1 | DB-probing health check |
| Middleware | 2 | Correlation ID generation, echo |

## Middleware Pipeline

The API uses a carefully ordered middleware pipeline for production-grade cross-cutting concerns:

```
UseExceptionHandler()        — Catches unhandled exceptions → JSON ProblemDetails 500
UseResponseCompression()     — Gzip / Brotli compression for responses
CorrelationIdMiddleware      — Reads/generates X-Correlation-ID, enriches log scope
RequestLoggingMiddleware     — Logs Method, Path, StatusCode, Duration for every request
UseHttpsRedirection()        — Redirects HTTP → HTTPS
UseCors()                    — Configurable CORS: AllowedOrigins from config; AllowAny only in Development
UseRouting()                 — MVC routing
UseAuthentication()          — JWT Bearer (registered only when Jwt:Authority is configured)
UseRateLimiter()             — Fixed-window rate limiting (100 req/min, partitioned per user → IP → anonymous)
UseAuthorization()           — Denies unauthenticated requests when auth is enabled; allows all in dev
UseEndpoints()               — Controller + health check endpoints
```

## Architecture

```
Order.WebAPI (Controllers, Validators, Middleware)
    │
    ▼
Order.Service (IOrderService → OrderService, business logic)
    │
    ▼
Order.Data (IOrderRepository → OrderRepository, EF Core DbContext)
    │
    ▼
Order.Model (DTOs, request/response records, shared constants)
```

Each layer depends only on the one directly below it. The DI container wires the interfaces to their implementations in `Startup.cs`.

## Project Structure

```
src/
├── Order.Data/                         EF Core DbContext, entities, repository
│   ├── Entities/                       EF Core entity classes
│   ├── EntityConfigurations/           Fluent API entity configurations
│   ├── Repositories/
│   │   ├── IOrderRepository.cs
│   │   └── OrderRepository.cs
│   ├── OrderContext.cs
│   ├── OrderMapper.cs                  Entity → DTO projections
│   └── PagingHelper.cs                 IQueryable page/take extension
├── Order.Model/                        Shared DTOs and request/response models
│   ├── CreateOrderItemRequest.cs
│   ├── CreateOrderRequest.cs
│   ├── CreateOrderResult.cs
│   ├── MonthlyProfit.cs
│   ├── OrderDetail.cs
│   ├── OrderItem.cs
│   ├── OrderStatusNames.cs
│   ├── OrderSummary.cs
│   ├── PagedResult.cs
│   ├── UpdateOrderStatusRequest.cs
│   └── UpdateOrderStatusResult.cs
├── Order.Service/                      Business logic layer
│   ├── IOrderService.cs
│   └── OrderService.cs
├── Order.Service.Tests/                NUnit unit tests — service + validator coverage
│   ├── ServiceTestBase.cs
│   ├── CreateOrderServiceTests.cs
│   ├── CreateOrderValidatorTests.cs
│   ├── GetOrderServiceTests.cs
│   ├── ProfitServiceTests.cs
│   ├── UpdateOrderStatusServiceTests.cs
│   └── UpdateOrderStatusValidatorTests.cs
├── Order.API.Tests/                    NUnit integration tests — WebApplicationFactory
│   ├── Helpers/
│   │   ├── ApiTestBase.cs
│   │   ├── OrderApiFactory.cs
│   │   ├── SeedData.cs
│   │   └── TestAuthHandler.cs
│   ├── CreateOrderTests.cs
│   ├── GetOrderTests.cs
│   ├── GetOrdersByStatusTests.cs
│   ├── HealthCheckTests.cs
│   ├── MiddlewareTests.cs
│   ├── ProfitTests.cs
│   └── UpdateOrderStatusTests.cs
├── Order.WebAPI/                       HTTP entry point
│   ├── Controllers/
│   │   └── OrderController.cs
│   ├── Middleware/
│   │   ├── CorrelationIdMiddleware.cs
│   │   ├── GlobalExceptionHandler.cs
│   │   └── RequestLoggingMiddleware.cs
│   ├── Validators/
│   │   ├── CreateOrderRequestValidator.cs
│   │   └── UpdateOrderStatusRequestValidator.cs
│   ├── Program.cs
│   └── Startup.cs
├── docs/api/                           Per-endpoint API documentation
│   ├── CreateOrder.md
│   ├── GetMonthlyProfit.md
│   ├── GetOrderById.md
│   ├── GetOrders.md
│   ├── GetOrdersByStatus.md
│   ├── Health.md
│   └── UpdateOrderStatus.md
├── Dockerfile
└── Order.sln
```

## Tech Stack

- **.NET 8** — LTS framework
- **ASP.NET Core MVC** — Controllers with `[ApiController]` attribute
- **EF Core 8** (Pomelo for MySQL, SQLite for tests)
- **FluentValidation** — Declarative request validation with auto-validation
- **NUnit** — Test framework
- **Swashbuckle** — Swagger/OpenAPI documentation (development only)
- **Docker** — MySQL + API containerised deployment

## API Documentation

See [docs/api/](docs/api/) for detailed per-endpoint documentation with request/response schemas and examples.

---

## Key Design Decisions

### Conditional JWT Authentication

`Jwt:Authority` is optional. When empty (local dev), the service skips JWT Bearer registration and overrides the default authorization policy with a pass-through assertion — all requests are permitted. When set (production), the JWT Bearer scheme is registered and the fallback policy enforces authentication on every endpoint. This allows `dotnet run` without a running IdP while keeping auth enforced in production with a single env-var change.

### ResellerId Index

A database index on `Order.ResellerId` is created in `OrderContext` and reflected in `mysql-init.sql`. Filtering by ResellerId is the dominant query pattern (`GET /orders/status/{status}` and `GET /orders`). Without this index, every such query is a full table scan at scale.

### Server-Side SQL Aggregations

`GetMonthlyProfitAsync` uses a `GroupBy` translated to a MySQL `GROUP BY` query, computing `SUM(Price - Cost)` in the database. The original implementation loaded all completed orders into memory first. The EF Core expression includes a SQLite-compatible fallback so unit tests don't require MySQL.

### Per-User Rate Limiting

The fixed-window rate limiter is partitioned by `HttpContext.User.Identity.Name` → client IP → `"anonymous"`. A global limit would allow one high-traffic client to exhaust the quota for all other users. Partitioning ensures fairness.

### ConcurrencyStamp State Management

Before calling `SaveChangesAsync` on a status update, the original `ConcurrencyStamp` is captured. On `DbUpdateConcurrencyException`, the stamp is reverted to its pre-update value. Without this, EF Core's change tracker holds a dirty entity — any subsequent operation in the same request scope would operate on corrupted state.

### Split Health Probes

- `/health/live` — no health checks registered; always returns `200 Healthy`. Used by the Kubernetes liveness probe. If this fails, the pod is restarted.
- `/health/ready` — runs the EF Core `DbContextCheck`. Used by the readiness probe. Returns `503` when the database is unavailable, removing the pod from the load balancer until connectivity is restored.

### Polly Retry on EF Core

`EnableRetryOnFailure(3, 5s)` wraps every EF Core command. MySQL connections can fail transiently (network blips, container restarts, connection pool exhaustion). Three retries with a 5-second back-off make the API resilient to momentary database unavailability without surfacing errors to callers.

### Configurable CORS

`AllowAnyOrigin` in production is a CORS misconfiguration. The API reads `Cors:AllowedOrigins` from configuration. When the array is non-empty, only those origins are permitted. `AllowAnyOrigin` is only active when the list is empty **and** the environment is `Development` — any other combination throws at startup, forcing explicit configuration.

