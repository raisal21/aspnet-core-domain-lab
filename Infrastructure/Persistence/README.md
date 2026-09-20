# Persistence boundary

The first persistence boundary uses one `DomainDbContext` for one PostgreSQL database. Business tables are mapped explicitly to their owning schema:

- `healthcare`
- `industrial`
- `logistics`
- `banking`
- `auth`

The schemas provide organizational ownership inside one database. They do not provide independent deployment, transaction, or security boundaries. A single context keeps the initial migration stream and transaction model small; a module can move to its own context later if the learning case requires that trade-off.

The EF migration history table stays in `public`. PostgreSQL schemas are created by the initial migration before module tables are added by the workflow migration. Do not use `EnsureCreated`; add workflow entities and migrations as each module is implemented.

`Database:ApplyMigrations` is opt-in. Compose enables it for local development; normal application startup does not silently mutate a database unless the setting is true.
