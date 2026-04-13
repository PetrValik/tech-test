# GET /orders/{orderId}

Returns the full detail of a single order, including all line items with pricing.

---

## Request

```
GET /api/v1/orders/{orderId}
```

### Route Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `orderId` | GUID | Yes | The unique identifier of the order |

---

## Response

### 200 OK

The response also includes an `ETag` header containing the current `ConcurrencyStamp`. Pass this as `If-Match` when calling `PATCH /orders/{id}/status` to enable optimistic concurrency.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "resellerId": "a1b2c3d4-0000-0000-0000-000000000001",
  "customerId": "a1b2c3d4-0000-0000-0000-000000000002",
  "statusId": "a1b2c3d4-0000-0000-0000-000000000003",
  "statusName": "Completed",
  "createdDate": "2024-03-01T12:00:00Z",
  "totalCost": 2.40,
  "totalPrice": 2.70,
  "items": [
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "serviceId": "b1b2c3d4-0000-0000-0000-000000000001",
      "serviceName": "Email",
      "productId": "c1b2c3d4-0000-0000-0000-000000000001",
      "productName": "100GB Mailbox",
      "quantity": 3,
      "unitCost": 0.80,
      "unitPrice": 0.90,
      "totalCost": 2.40,
      "totalPrice": 2.70
    }
  ]
}
```

### 404 Not Found

Returned when no order with the given GUID exists, or the order has been soft-deleted.

---

## Files in this slice

| File | Purpose |
|---|---|
| `GetOrderByIdEndpoint.cs` | GET /api/v1/orders/{orderId} — sets ETag header, returns 404 when not found |
| `GetOrderByIdHandler.cs` | Loads order with Status, Items, Product, and Service |

The handler loads the order with:

```csharp
.Include(x => x.Status)
.Include(x => x.Items).ThenInclude(i => i.Product)
.Include(x => x.Items).ThenInclude(i => i.Service)
```

`TotalCost` and `TotalPrice` are computed in memory from the loaded item list to maintain SQLite compatibility.
