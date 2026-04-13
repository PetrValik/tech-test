# GET /orders/search

Searches orders using any combination of optional filters. All filters are applied server-side in a single SQL query. When no filters are supplied, the result is equivalent to `GET /orders`.

This endpoint is subject to the **`expensive`** rate-limiting policy (10 req/min) because the query may involve full-table price aggregations.

Results are output-cached under the `"orders"` tag for 1 minute. The cache is shared across API replicas when Redis is configured.

---

## Request

```
GET /api/v1/orders/search
```

### Query Parameters

All parameters are optional.

| Parameter | Type | Description |
|---|---|---|
| `from` | `DateTime` | Include only orders created on or after this UTC date. |
| `to` | `DateTime` | Include only orders created on or before this UTC date. |
| `resellerId` | `Guid` | Filter by reseller ID. |
| `customerId` | `Guid` | Filter by customer ID. |
| `status` | `string` | Filter by status name (case-insensitive). Valid values: `Created`, `In Progress`, `Failed`, `Completed`. |
| `minTotal` | `decimal` | Include only orders whose `TotalPrice` is ≥ this value. |
| `maxTotal` | `decimal` | Include only orders whose `TotalPrice` is ≤ this value. |
| `page` | `int` | Page number (default `1`, range 1–1,000,000). |
| `pageSize` | `int` | Items per page (default `50`, range 1–200). |

---

## Validation

| Rule | Detail |
|---|---|
| `status` (if provided) | Must be one of `Created`, `In Progress`, `Failed`, `Completed` (case-insensitive). An unknown value returns `400`. |
| `from` / `to` (if both provided) | `from` must not be later than `to`. |
| `page` / `pageSize` | Standard pagination constraints (see above). |

---

## Response

### 200 OK

Same `PagedResult<OrderSummaryResponse>` envelope as `GET /orders`.

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "resellerId": "a1b2c3d4-0000-0000-0000-000000000001",
      "customerId": "a1b2c3d4-0000-0000-0000-000000000002",
      "statusId": "a1b2c3d4-0000-0000-0000-000000000003",
      "statusName": "Completed",
      "createdDate": "2024-03-01T12:00:00Z",
      "itemCount": 2,
      "totalCost": 80.00,
      "totalPrice": 100.00
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

Returns an empty `items` array when no orders match the supplied filters.

### 400 Bad Request — Validation failure

```json
{
  "Status": ["Status must be one of: Created, In Progress, Failed, Completed"],
  "From": ["From must not be later than To."]
}
```

### 400 Bad Request — Pagination out of range

```json
{ "error": "page must be between 1 and 1,000,000; pageSize must be between 1 and 200." }
```

---

## Example

```bash
# Orders for a specific customer in Completed status, total ≥ 100, page 2
curl "http://localhost:8001/api/v1/orders/search?customerId=a1b2c3d4-0000-0000-0000-000000000002&status=Completed&minTotal=100&page=2&pageSize=25"
```

---

## Files in this slice

| File | Purpose |
|---|---|
| `SearchOrdersQuery.cs` | MediatR query (all filter fields + pagination) |
| `SearchOrdersValidator.cs` | FluentValidation rules (status enum, from ≤ to) |
| `SearchOrdersEndpoint.cs` | GET /api/v1/orders/search — binds all query params, dispatches query |
| `SearchOrdersHandler.cs` | Builds server-side SQL predicate, pages and projects results |

Handler flow:

1. Builds a base `IQueryable<Order>` by progressively adding `WHERE` clauses for each non-null filter (`From`, `To`, `ResellerId`, `CustomerId`, `Status`).
2. Projects to an anonymous type including computed `TotalCost` and `TotalPrice` (aggregated from `Items`).
3. Applies `MinTotal` and `MaxTotal` filters **after** the price projection, because EF Core must compute totals before they can be compared.
4. Counts total matching rows for the `totalCount` field.
5. Orders by `CreatedDate` descending, then applies `Skip/Take`.
6. Maps to `OrderSummaryResponse`.
