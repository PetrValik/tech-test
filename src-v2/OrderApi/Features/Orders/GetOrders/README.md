# GET /orders

Returns a paginated list of all orders, sorted newest-first.

---

## Request

```
GET /api/v1/orders?page={page}&pageSize={pageSize}
```

### Query Parameters

| Parameter | Type | Required | Default | Constraints |
|---|---|---|---|---|
| `page` | int | No | `1` | 1–1,000,000 |
| `pageSize` | int | No | `50` | 1–200 |

---

## Response

### 200 OK

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
  "totalCount": 42,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

### 400 Bad Request

Returned when pagination parameters are out of range.

```json
{ "error": "page must be between 1 and 1,000,000; pageSize must be between 1 and 200." }
```

---

## Files in this slice

| File | Purpose |
|---|---|
| `GetOrdersEndpoint.cs` | GET /api/v1/orders — validates pagination, dispatches query |
| `GetOrdersHandler.cs` | Delegates to OrderProjections.ToPagedSummaryAsync |

`GetOrdersHandler` delegates to `OrderProjections.ToPagedSummaryAsync`, which:

1. Counts total rows matching the query.
2. Applies `OrderByDescending(x => x.CreatedDate)` + `Skip/Take`.
3. Eager-loads `Status` and `Items.Product`.
4. Aggregates `TotalCost` and `TotalPrice` in memory (SQLite compatibility).
