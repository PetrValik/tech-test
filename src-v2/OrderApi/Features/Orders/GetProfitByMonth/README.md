# GET /orders/profit/monthly

Returns monthly profit totals aggregated from all `Completed` orders.

Profit per item = `quantity × (unitPrice − unitCost)`.

---

## Request

```
GET /api/v1/orders/profit/monthly
```

No parameters.

---

## Response

### 200 OK

```json
[
  {
    "year": 2024,
    "month": 1,
    "totalProfit": 12.40
  },
  {
    "year": 2024,
    "month": 2,
    "totalProfit": 8.70
  }
]
```

Results are ordered chronologically (year ASC, month ASC).  
Returns an empty array when there are no `Completed` orders.

---

## Files in this slice

| File | Purpose |
|---|---|
| `GetProfitByMonthEndpoint.cs` | GET /api/v1/orders/profit/monthly — dispatches query |
| `GetProfitByMonthHandler.cs` | Aggregates profit, dual code path for MySQL vs SQLite |

The handler scopes results to the most recent 24 months and uses two code paths:

**MySQL (production):** issues a single server-side `GROUP BY` query — only the aggregated rows are transferred to the application; no individual item rows are loaded into memory.

**SQLite (integration tests):** falls back to loading the filtered item rows and grouping client-side, because SQLite does not support the date-part functions used in the EF Core LINQ expression.
