# Healthcare

## Workflow surface

- `POST/GET /api/v1/healthcare/patients`
- `POST/GET /api/v1/healthcare/patients/{patientId}/appointments`
- `POST/GET /api/v1/healthcare/patients/{patientId}/lab-results`
- `POST /api/v1/healthcare/lab-results/{labResultId}/review`
- `POST/GET /api/v1/healthcare/inventory`
- `POST /api/v1/healthcare/inventory/{itemId}/adjust`

All endpoints require the synthetic healthcare policies. Reads use `healthcare.read`; writes and reviews use `healthcare.write`; `platform.admin` bypasses both.

## Learning trade-offs

The module keeps patient, appointment, lab, and inventory tables in the `healthcare` schema while sharing one EF Core context. This keeps the learning project small but leaves ownership explicit in mappings. Domain rules reject duplicate medical-record numbers, past appointments, repeated lab reviews, and negative inventory.

Healthcare errors use `errorCode`, `message`, `patientReference`, and optional field `errors`. Validation is HTTP 400; missing resources are HTTP 404; state conflicts are HTTP 409; impossible domain values are HTTP 422. No real PII or clinical interpretation is stored.
