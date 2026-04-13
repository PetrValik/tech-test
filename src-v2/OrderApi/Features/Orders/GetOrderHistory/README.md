# GET /orders/{orderId}/history

Returns the paginated status-change audit trail for a specific order. Each entry records the previous status (`FromStatus`), the new status (`ToStatus`), and the UTC timestamp of the transition. Entries are returned in chronological order (oldest first).

An order that has never had a status transition returns an empty `items` array.

---

## Request

```
GET /api/v1/orders/{orderId}/history?page={page}&pageSize={pageSize}
```

### Path Parameters

| Parameter | Type | Description |
|---|---|---|
| `orderId` | GUID | The unique identifier of the order. |

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
      "fromStatus": "Created",
      "toStatus": "In Progress",
      "changedAt": "2024-03-02T09:00:00Z"
    },
    {
      "fromStatus": "In Progress",
      "toStatus": "Completed",
      "changedAt": "2024-03-03T14:30:00Z"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

Returns an empty `items` array when the order exists but has no recorded transitions.

### 400 Bad Request

Returned when pagination parameters are out of range.

```json
{ "error": "page must be between 1 and 1,000,000; pageSize must be between 1 and 200." }
```

---

## Example

```bash
curl "http://localhost:8001/api/v1/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6/history"
```

---

## Files in this slice

| File | Purpose |
|---|---|
| `GetOrderHistoryQuery.cs` | MediatR query (OrderId, Page, PageSize) |
| `OrderStatusHistoryResponse.cs` | Response record (FromStatus, ToStatus, ChangedAt) |
| `GetOrderHistoryEndpoint.cs` | GET /api/v1/orders/{orderId}/history — validates pagination, dispatches query |
| `GetOrderHistoryHandler.cs` | Queries StatusHistory, orders by ChangedAt ascending, pages results |

Handler flow:

1. Filters `StatusHistory` rows by `OrderId` (as `byte[]`).
2. Counts total matching rows.
3. Orders by `ChangedAt` ascending (chronological), then applies `Skip/Take`.
4. Projects each row to `OrderStatusHistoryResponse(FromStatus.Name, ToStatus.Name, ChangedAt)`.

> This endpoint does not use output caching because status history is append-only and changes immediately after every `PATCH /orders/{id}/status`.
