# GET /orders/deleted

Returns a paginated list of soft-deleted orders. Soft-deleted records are excluded from all standard list and detail endpoints; this endpoint exposes the archive for administrative review and audit purposes.

---

## Request

```
GET /api/v1/orders/deleted?page={page}&pageSize={pageSize}
```

### Query Parameters

| Parameter | Type | Required | Default | Constraints |
|---|---|---|---|---|
| `page` | int | No | `1` | 1–1,000,000 |
| `pageSize` | int | No | `50` | 1–200 |

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
      "statusName": "Created",
      "createdDate": "2024-03-01T12:00:00Z",
      "itemCount": 2,
      "totalCost": 1.60,
      "totalPrice": 1.80
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

Returns an empty `items` array when no soft-deleted orders exist.

### 400 Bad Request

Returned when pagination parameters are out of range.

```json
{ "error": "page must be between 1 and 1,000,000; pageSize must be between 1 and 200." }
```

---

## Example

```bash
curl "http://localhost:8001/api/v1/orders/deleted?page=1&pageSize=20"
```

---

## Files in this slice

| File | Purpose |
|---|---|
| `GetDeletedOrdersQuery.cs` | MediatR query (Page, PageSize) |
| `GetDeletedOrdersEndpoint.cs` | GET /api/v1/orders/deleted — validates pagination, dispatches query |
| `GetDeletedOrdersHandler.cs` | Bypasses global IsDeleted filter, pages results |

Handler flow:

1. Builds an `IQueryable` against `Orders` with `IgnoreQueryFilters()` and a `Where(o => o.IsDeleted)` predicate.
2. Delegates to `OrderProjections.ToPagedSummaryAsync` for consistent count + page + projection logic.
