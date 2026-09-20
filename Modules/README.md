# Modules

The application stays in one Web API project, with physical folders and namespaces separating the modules. Each module owns its endpoints, workflow orchestration, DTOs, domain errors, and explicit schema mappings inside the shared context. Shared host infrastructure and authentication remain outside the business module folders.

All module endpoints are versioned under `/api/v1` and documented in the generated OpenAPI document with module tags.

## Healthcare

Patient and appointment, lab result review, and medical inventory.

## Industrial Automation

Asset and device status, telemetry and alarms, maintenance work orders, and device commands.

## Logistics

Shipment tracking, warehouse inventory, delivery routes and stops, and the carrier integration boundary.

## Banking

Customer and account, payment and transfer, statement and transaction history, and fraud/risk review.
