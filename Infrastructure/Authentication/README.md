# Local authentication

This module is a synthetic local JWT issuer for learning. It is not a production identity system.

- Users, roles, and refresh-token hashes are stored in the `auth` PostgreSQL schema.
- Passwords are stored as ASP.NET Core Identity password hashes.
- Access tokens use an HMAC-SHA256 signing key from the `Jwt` configuration section.
- Refresh tokens are random high-entropy values. Only SHA-256 hashes are persisted.
- Refresh rotates the presented token. A concurrency version allows only one simultaneous request to rotate a token.
- Reusing a revoked token revokes the user's active refresh tokens.
- `POST /api/v1/auth/revoke` revokes one refresh token for the authenticated user.
- Revoking a refresh token does not invalidate already issued access JWTs; they remain valid until their short expiry.
- Domain policies use roles such as `healthcare.read` and `banking.transfer`.

Compose enables synthetic development seeding. The credentials are documented in `README.md` and must not be reused outside this learning repository.
