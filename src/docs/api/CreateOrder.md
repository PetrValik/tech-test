# POST /orders

Creates a new order with one or more line items.

## Request

```
POST /orders
Content-Type: application/json
```

### Body

```json
{
  "resellerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "items": [
    { "productId": "existing-product-guid", "quantity": 5 }
  ]
}
```

### Validation Rules

| Field | Rule |
|-------|------|
| `resellerId` | Required, non-empty GUID |
| `customerId` | Required, non-empty GUID |
| `items` | Required, 1–100 items, no null entries, no duplicate ProductIds |
| `items[].productId` | Required, non-empty GUID, must exist in database |
| `items[].quantity` | 1 – 1,000,000 |

## Response

**201 Created**

```json
{ "id": "new-order-guid" }
```

Includes a `Location` header pointing to `GET /orders/{id}`.

**400 Bad Request** — Validation failure or invalid product IDs.

```json
{
  "error": "One or more product IDs are invalid.",
  "invalidProductIds": ["non-existent-guid"]
}
```

## Example

```bash
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{
    "resellerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "items": [
      { "productId": "existing-product-guid", "quantity": 5 }
    ]
  }'
```
