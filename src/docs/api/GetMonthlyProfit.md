# GET /orders/profit/monthly

Returns total profit grouped by year and month for all Completed orders.

Profit is calculated as `SUM(quantity × (unitPrice − unitCost))` for each calendar month.

## Request

```
GET /orders/profit/monthly
```

No query parameters.

## Response

**200 OK**

```json
[
  { "year": 2025, "month": 1, "totalProfit": 150.00 },
  { "year": 2025, "month": 2, "totalProfit": 230.50 }
]
```

Results are sorted by year and month ascending. Returns an empty array `[]` when there are no Completed orders.

## Example

```bash
curl http://localhost:5000/orders/profit/monthly
```
