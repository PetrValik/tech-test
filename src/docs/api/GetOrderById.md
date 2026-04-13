# GET /orders/{id}

Returns the full detail of a single order, including all line items.

## Request

```
GET /orders/{orderId}
```

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `orderId` | GUID | Path | The unique identifier of the order |

## Response

**200 OK**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "resellerId": "...",
  "customerId": "...",
  "statusName": "Created",
  "createdDate": "2025-01-15T10:30:00Z",
  "totalCost": 24.00,
  "totalPrice": 27.00,
  "items": [
    {
      "productId": "...",
      "productName": "100GB Mailbox",
      "quantity": 3,
      "unitCost": 0.80,
      "unitPrice": 0.90
    }
  ]
}
```

**404 Not Found** — When no order exists with the given ID.

## Example

```bash
curl http://localhost:5000/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6
```
