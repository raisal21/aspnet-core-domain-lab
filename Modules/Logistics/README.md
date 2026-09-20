# Logistics

## Workflow surface

- `POST/GET /api/v1/logistics/shipments/{trackingNumber}` and `POST .../status`
- `POST/GET /api/v1/logistics/warehouse/.../stock`
- `POST /api/v1/logistics/warehouse/stock/{stockId}/adjust`
- `POST /api/v1/logistics/routes` and `POST .../stops`
- `POST /api/v1/logistics/stops/{stopId}/complete`
- `POST /api/v1/logistics/shipments/{trackingNumber}/carrier-events`

Reads use `logistics.read`; dispatch and integration writes use `logistics.dispatcher`; `platform.admin` bypasses both. Dispatch writes have an independent fixed-window rate limit.

## Learning trade-offs

Shipment reads are cached in memory for 15 seconds and warehouse reads for five seconds. Carrier events are an idempotent boundary keyed by the external reference; the application stores a compact synthetic payload summary instead of calling a real carrier. The shared EF context maps shipments, warehouse stock, routes, stops, and carrier events to the `logistics` schema.

Logistics errors use `errorCode`, `message`, `trackingNumber`, `retryable`, and optional field `errors`. Duplicate keys and invalid route state are 409; missing shipments are 404; invalid quantities/statuses are 422; an unknown carrier shipment is retryable and returns 404. Cached reads trade freshness for a clear cache-invalidation example.
