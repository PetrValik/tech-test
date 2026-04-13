# GET /orders

Returns a paginated summary list of all orders, sorted by creation date (newest first).

## Request

```
GET /orders?page={page}&pageSize={pageSize}
```

### Query Parameters

| Parameter | Type | Required | Default | Constraints |
|---|---|---|---|---|
| `page` | int | No | `1` | ≥ 1 |
| `pageSize` | int | No | `50` | 1–200 |

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
      "statusName": "Created",
      "createdDate": "2025-01-15T10:30:00Z",
      "itemCount": 3,
      "totalCost": 24.00,
      "totalPrice": 27.00
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 50
}
```

Returns an empty `items` array when no orders exist.

## Example

```bash
curl "http://localhost:8000/orders?page=1&pageSize=20"
```
