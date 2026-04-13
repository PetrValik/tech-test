# POST /orders

Creates a new order with one or more line items.

The order is assigned the `Created` status automatically.

---

## Request

```
POST /api/v1/orders
Content-Type: application/json
```

### Body

```json
{
  "resellerId": "a1b2c3d4-0000-0000-0000-000000000001",
  "customerId": "a1b2c3d4-0000-0000-0000-000000000002",
  "items": [
    {
      "productId": "c1b2c3d4-0000-0000-0000-000000000001",
      "quantity": 3
    },
    {
      "productId": "c1b2c3d4-0000-0000-0000-000000000002",
      "quantity": 1
    }
  ]
}
```

### Validation Rules (FluentValidation)

| Field | Rule |
|---|---|
| `resellerId` | Required, non-empty GUID |
| `customerId` | Required, non-empty GUID |
| `items` | Required, non-null, 1–100 items, no null entries, no duplicate ProductIds |
| `items[].productId` | Required, non-empty GUID |
| `items[].quantity` | Must be 1–1,000,000 |

A `null` request body also returns `400`.

---

## Response

### 201 Created

```
Location: /orders/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

```json
{ "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

### 400 Bad Request — Validation failure

```json
{
  "ResellerId": ["ResellerId is required."],
  "Items": ["Order must contain at least one item."]
}
```

### 400 Bad Request — Invalid product IDs

```json
{
  "error": "One or more product IDs are invalid.",
  "invalidProductIds": ["c1b2c3d4-0000-0000-0000-000000000099"]
}
```

---

## Files in this slice

| File | Purpose |
|---|---|
| `CreateOrderCommand.cs` | MediatR command (resellerId, customerId, items) |
| `CreateOrderItemRequest.cs` | Nested item record (productId, quantity) |
| `CreateOrderEndpoint.cs` | POST /api/v1/orders — validates body + dispatches command |
| `CreateOrderResult.cs` | Result with Success flag and InvalidProductIds list |
| `CreateOrderValidator.cs` | FluentValidation rules |
| `CreateOrderHandler.cs` | Business logic |

Handler flow:

1. Converts all `ProductId` GUIDs to `byte[]` and queries `OrderProducts` in a single `WHERE Id IN (...)`.
2. Identifies any IDs not found in the database → returns `400` immediately.
3. Looks up the `Created` status record.
4. Creates one `Order` row and one `OrderItem` row per item.
5. Saves with `SaveChangesAsync`.
