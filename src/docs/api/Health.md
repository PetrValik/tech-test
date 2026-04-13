# GET /health

Two health check endpoints are exposed for container orchestration.

## Liveness — GET /health/live

Always returns `200 Healthy`. No dependency checks are performed. Used by Kubernetes liveness probes — if this fails, the pod is restarted.

```bash
curl http://localhost:8000/health/live
# Response: Healthy
```

## Readiness — GET /health/ready

Runs the EF Core `DbContextCheck` against MySQL. Returns `503 Service Unavailable` when the database is unreachable, removing the pod from the load balancer until connectivity is restored.

```bash
curl http://localhost:8000/health/ready
# Response (healthy):   Healthy
# Response (unhealthy): Unhealthy
```

## Response Format

Both endpoints return plain text (`text/plain`), not JSON.

| Status | Meaning |
|--------|---------|
| `200 Healthy` | All checks passed (or no checks for `/live`) |
| `503 Unhealthy` | One or more checks failed |
