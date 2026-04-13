# GET /orders/status/{statusName}

Returns a paginated list of orders matching the specified status name.

## Request

```
GET /orders/status/{statusName}?page={page}&pageSize={pageSize}
```

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `statusName` | string | Path | Exact status name (case-sensitive) |
| `page` | int | Query | Page number (default `1`, ≥ 1) |
| `pageSize` | int | Query | Items per page (default `50`, 1–200) |

Valid values for `statusName`: `Created`, `In Progress`, `Failed`, `Completed`

## Response

**200 OK**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "resellerId": "aabbccdd-0000-0000-0000-000000000001",
      "customerId": "aabbccdd-0000-0000-0000-000000000002",
      "statusId": "aabbccdd-0000-0000-0000-000000000010",
      "statusName": "Completed",
      "createdDate": "2025-01-15T10:30:00Z",
      "itemCount": 2,
      "totalCost": 16.00,
      "totalPrice": 18.00
    }
  ],
  "totalCount": 12,
  "page": 1,
  "pageSize": 50
}
```

Returns an empty `items` array when no orders match the status.

**400 Bad Request** — returned when `statusName` is null or whitespace.

## Example

```bash
curl "http://localhost:8000/orders/status/Completed?page=1&pageSize=25"
```
