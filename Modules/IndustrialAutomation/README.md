# Industrial Automation

## Workflow surface

- `POST/GET /api/v1/industrial/assets`
- `GET /api/v1/industrial/assets/{assetId}` and `POST .../status`
- `POST/GET /api/v1/industrial/assets/{assetId}/telemetry`
- `GET /api/v1/industrial/assets/{assetId}/alarms`
- `POST /api/v1/industrial/alarms/{alarmId}/acknowledge`
- `POST/GET /api/v1/industrial/assets/{assetId}/maintenance`
- `POST /api/v1/industrial/maintenance/{workOrderId}/complete`
- `POST /api/v1/industrial/assets/{assetId}/commands`

Operators use `industrial.operate`; maintainers use `industrial.maintain`; `platform.admin` bypasses both. Device commands have a fixed-window rate limit and return `accepted-simulation-only`; they never actuate a device.

## Learning trade-offs

Telemetry is cached in memory for three seconds and large telemetry responses use response compression. Commands use an idempotency key and a three-request burst limit. A value at or above the synthetic threshold raises an alarm. The shared EF context maps assets, telemetry, alarms, work orders, and commands to the `industrial` schema.

Industrial errors use `errorCode`, `message`, `assetCode`, `severity`, and optional field `errors`. Missing assets and alarms are 404; invalid status transitions are 409 or 422; rate-limit rejection is 429. The simulation records commands but does not connect to hardware.
