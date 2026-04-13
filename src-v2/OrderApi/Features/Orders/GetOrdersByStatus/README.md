# GET /orders/status/{statusName}

Returns a paginated list of orders filtered by status name. Case-insensitive.

---

## Request

```
GET /api/v1/orders/status/{statusName}?page={page}&pageSize={pageSize}
```

### Route Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `statusName` | string | Yes | One of: `Created`, `In Progress`, `Failed`, `Completed` |

### Query Parameters

| Parameter | Type | Required | Default | Constraints |
|---|---|---|---|---|
| `page` | int | No | `1` | >= 1 |
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
      "statusName": "In Progress",
      "createdDate": "2024-03-01T12:00:00Z",
      "itemCount": 1,
      "totalCost": 0.80,
      "totalPrice": 0.90
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

Returns an empty `items` array (not 404) when the status name is valid but no orders match, or when an unrecognised status name is provided.

### 400 Bad Request

Returned when pagination parameters are out of range.

---

## Files in this slice

| File | Purpose |
|---|---|
| `GetOrdersByStatusEndpoint.cs` | GET /api/v1/orders/status/{statusName} — validates pagination, dispatches query |
| `GetOrdersByStatusHandler.cs` | Normalises status name, queries and pages matching orders |

The handler normalises `statusName` against `OrderStatusNames.All` using a case-insensitive match before querying, so `"created"` and `"CREATED"` both return `Created` orders.
