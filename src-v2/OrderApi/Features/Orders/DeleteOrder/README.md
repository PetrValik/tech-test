# DELETE /orders/{orderId}

Soft-deletes an order. The record is **not** physically removed from the database — `IsDeleted` is set to `true` and `DeletedAt` is stamped with the current UTC time. The order becomes invisible to all standard list and detail endpoints but remains accessible via `GET /orders/deleted`.

The operation is idempotent: calling `DELETE` on an already-deleted order returns `204 No Content` without error.

---

## Request

```
DELETE /api/v1/orders/{orderId}
```

### Path Parameters

| Parameter | Type | Description |
|---|---|---|
| `orderId` | GUID | The unique identifier of the order to soft-delete. |

---

## Response

### 204 No Content

The order was successfully soft-deleted (or was already deleted).

### 404 Not Found

No order with the supplied ID exists in the database (including the soft-deleted archive).

```json
{}
```

---

## Side Effects

| Effect | Detail |
|---|---|
| Output cache eviction | The `"orders"` tag is evicted from the output cache on success, so subsequent list/detail requests reflect the deletion immediately. |
| Domain event | `OrderDeletedEvent` is published via `IOrderEventPublisher` after `SaveChangesAsync` completes. |

---

## Example

```bash
curl -X DELETE http://localhost:8001/api/v1/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6
# 204 No Content on success, 404 if the order ID does not exist
```

---

## Files in this slice

| File | Purpose |
|---|---|
| `DeleteOrderCommand.cs` | MediatR command (OrderId GUID) |
| `DeleteOrderResult.cs` | Enum: Deleted \| NotFound |
| `DeleteOrderEndpoint.cs` | DELETE /api/v1/orders/{orderId} — dispatches command, evicts cache |
| `DeleteOrderHandler.cs` | Sets IsDeleted + DeletedAt, publishes OrderDeletedEvent |

Handler flow:

1. Loads the order by ID using `IgnoreQueryFilters()` so the soft-delete filter does not hide already-deleted records.
2. Returns `NotFound` if the record does not exist.
3. Returns `Deleted` immediately (idempotent) if `IsDeleted` is already `true`.
4. Sets `IsDeleted = true` and `DeletedAt = UtcNow`, then calls `SaveChangesAsync`.
5. Publishes `OrderDeletedEvent` containing the `OrderId` and `DeletedAt` timestamp.
