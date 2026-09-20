# Banking

## Workflow surface

- `POST /api/v1/banking/customers`
- `POST /api/v1/banking/customers/{customerId}/accounts` and `GET /api/v1/banking/accounts/{accountId}`
- `POST /api/v1/banking/transfers`
- `GET /api/v1/banking/accounts/{accountId}/transactions`
- `POST /api/v1/banking/risk-reviews`
- `POST /api/v1/banking/risk-reviews/{reviewId}/decision`

Customer/account and statement reads require `banking.read`; transfers require `banking.transfer`; risk review requires `banking.risk`; `platform.admin` bypasses all. Transfers have an independent rate limit and do not use response compression because the example emphasizes sensitive response handling.

## Learning trade-offs

Transfers run in an EF Core transaction, update two synthetic account balances, write debit/credit history, and require an idempotency key. Account versions provide optimistic concurrency protection. Risk review is a separate workflow and does not represent a real fraud decision. All values are synthetic minor units in the `SIM` currency.

Banking errors use `errorCode`, `message`, `accountReference`, and optional `violations`. Insufficient balance and currency problems are 422; duplicate idempotency data is 409; missing resources are 404; concurrent balance conflicts are retryable 409 responses. No real money, customer data, or payment network is involved.
