# PATCH /orders/{orderId}/status

Updates the status of an existing order.

---

## Request

```
PATCH /api/v1/orders/{orderId}/status
Content-Type: application/json
If-Match: "current-etag-value"   (optional — enables optimistic concurrency)
```

### Route Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `orderId` | GUID | Yes | The unique identifier of the order |

### Headers

| Header | Required | Description |
|---|---|---|
| `If-Match` | No | ETag of the order retrieved from a previous GET. When provided, the update is rejected with `409 Conflict` if the order has been modified since the ETag was issued. |

### Body

```json
{
  "statusName": "Completed"
}
```

### Validation Rules (FluentValidation)

| Field | Rule |
|---|---|
| `statusName` | Required; must be one of: `Created`, `In Progress`, `Failed`, `Completed` |

---

## Response

### 204 No Content

The status was updated successfully. No body.

### 400 Bad Request — Validation failure

```json
{
  "StatusName": ["StatusName must be one of: Created, In Progress, Failed, Completed"]
}
```

### 404 Not Found

Returned when no order with the given GUID exists.

### 409 Conflict — Optimistic concurrency failure

Returned when the `If-Match` header does not match the order's current `ConcurrencyStamp`.

```json
{ "error": "The order has been modified. Refresh and retry." }
```

---

## Files in this slice

| File | Purpose |
|---|---|
| `UpdateOrderStatusCommand.cs` | MediatR command (orderId, statusName, optional ETag) |
| `UpdateOrderStatusRequest.cs` | HTTP request body DTO (statusName) |
| `UpdateOrderStatusEndpoint.cs` | PATCH /api/v1/orders/{orderId}/status |
| `UpdateOrderStatusValidator.cs` | FluentValidation whitelist check against `OrderStatusNames.All` |
| `UpdateOrderStatusHandler.cs` | DB lookup + update + audit trail, returns `UpdateResult` |
| `UpdateResult.cs` | Discriminated result: `Success`, `OrderNotFound`, `InvalidStatus`, `Conflict` |

Handler flow:

1. Validates `StatusName` against `OrderStatusNames.All` (case-insensitive) — returns `InvalidStatus` if not found.
2. Looks up the `OrderStatus` row and the `Order` by ID.
3. If `If-Match` was supplied and does not match `order.ConcurrencyStamp`, returns `Conflict`.
4. Writes an `OrderStatusHistory` audit entry for the transition.
5. Updates `order.StatusId` and regenerates `ConcurrencyStamp`, then saves inside a transaction.
6. Publishes an `OrderStatusChangedEvent`.

> The validator performs a fast whitelist check before the handler runs, avoiding a database round-trip for clearly invalid status names.
