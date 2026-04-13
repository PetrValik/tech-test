# PATCH /orders/{id}/status

Updates the status of an existing order.

## Request

```
PATCH /orders/{orderId}/status
Content-Type: application/json
```

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `orderId` | GUID | Path | The unique identifier of the order to update |

### Body

```json
{ "statusName": "Completed" }
```

### Validation Rules

| Field | Rule |
|-------|------|
| `statusName` | Required, must be one of: `Created`, `In Progress`, `Failed`, `Completed` (case-sensitive) |

## Response

**204 No Content** — Status updated successfully.

**400 Bad Request** — Invalid or empty status name.

**404 Not Found** — Order does not exist.

## Example

```bash
curl -X PATCH http://localhost:5000/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6/status \
  -H "Content-Type: application/json" \
  -d '{ "statusName": "Completed" }'
```
