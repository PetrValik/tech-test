# Health Check Endpoints

Two endpoints are exposed for container orchestration and monitoring.

---

## GET /health/live

Liveness probe — used by Kubernetes to determine whether the pod should be restarted.

Returns `200 OK` as long as the process is alive. No dependency checks are performed so the pod is never removed from the load balancer due to a transient database or cache outage. A non-200 response causes Kubernetes to restart the container.

```
GET /health/live
```

### Response

```json
{ "status": "Healthy" }
```

Always returns `200 OK`.

---

## GET /health/ready

Readiness probe — used by Kubernetes to determine whether the pod is ready to serve traffic.

Runs all registered health checks (always the database context check; optionally a Redis check when `Redis:ConnectionString` is configured). Returns `503 Service Unavailable` when any check fails, removing the pod from the load balancer until its dependencies recover.

```
GET /health/ready
```

### 200 OK — All checks healthy

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "OrderContext", "status": "Healthy" }
  ]
}
```

### 503 Service Unavailable — One or more checks failed

```json
{
  "status": "Unhealthy",
  "checks": [
    { "name": "OrderContext", "status": "Unhealthy" }
  ]
}
```

---

## Files in this slice

| File | Purpose |
|---|---|
| `LivenessHealthEndpoint.cs` | GET /health/live — `Predicate = _ => false`, always returns Healthy |
| `ReadinessHealthEndpoint.cs` | GET /health/ready — runs all registered checks, returns status and check details |

Health checks registered in `AddPersistence`:

- `AddDbContextCheck<OrderContext>()` — always registered
- `AddRedis(connectionString)` — registered only when `Redis:ConnectionString` is set
