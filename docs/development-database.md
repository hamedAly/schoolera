# Development database guide

Schoolera uses **EF Core + SQL Server** in `Schoolera.Infrastructure`.

## Provider and connection string

| Item | Value |
|------|-------|
| Provider | Microsoft.EntityFrameworkCore.SqlServer |
| Config key | `ConnectionStrings:DefaultConnection` |
| Loaded from | `appsettings.json` / `appsettings.Development.json` / environment / user secrets |

### Examples (no real secrets)

Local default instance (Windows integrated auth):

```text
Data Source=.;Initial Catalog=SchooleraDb;Integrated Security=True;TrustServerCertificate=True
```

LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=SchooleraDb;Trusted_Connection=True;TrustServerCertificate=True
```

Do **not** commit passwords or production connection strings.

## Database startup flags

Configuration section: `Database`

| Key | Development (`appsettings.Development.json`) | Production-safe base (`appsettings.json`) |
|-----|----------------------------------------------|-------------------------------------------|
| `ApplyMigrations` | `true` | `false` |
| `SeedData` | `true` | `false` |

### Lifecycle

1. Application builds the DI container.
2. `InitializeDatabaseAsync()` runs before the HTTP pipeline.
3. If `ApplyMigrations=true`, `Database.MigrateAsync()` runs on a scoped `DbContext`.
4. Migration success/failure/disabled is logged. On failure, startup **stops** (exception rethrown).
5. If `SeedData=true` and migrations did not fail, seed runs.
6. Seed is idempotent (schools matched by stable **Name** lookup).
7. Never use `EnsureCreated` for schema management.

Disable either flag with environment variables:

```powershell
$env:Database__ApplyMigrations="false"
$env:Database__SeedData="false"
```

## EF CLI commands

From the repository root (`Schoolera.slnx` location):

```powershell
dotnet tool restore

# List migrations
dotnet ef migrations list `
  --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj `
  --startup-project src/Schoolera.Api/Schoolera.Api.csproj

# Add a migration
dotnet ef migrations add MigrationName `
  --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj `
  --startup-project src/Schoolera.Api/Schoolera.Api.csproj `
  --output-dir Persistence/Migrations

# Apply migrations manually
dotnet ef database update `
  --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj `
  --startup-project src/Schoolera.Api/Schoolera.Api.csproj

# Remove the last unapplied migration (do not use against production DBs carelessly)
dotnet ef migrations remove `
  --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj `
  --startup-project src/Schoolera.Api/Schoolera.Api.csproj
```

## Seed behavior

- Controlled by `Database:SeedData`.
- Runs only after successful migration when both enabled.
- Inserts a small Development school set if missing by **Name**.
- Does not delete or overwrite existing rows.
- Safe to run on every Development startup.

## File storage (related local data)

Uploads are **not** stored under Angular `wwwroot`.

| Setting | Default |
|---------|---------|
| Physical root | `App_Data/uploads` (under API content root) |
| Public URL prefix | `/uploads` |
| Config section | `FileStorage` |

Uploaded binary contents under `App_Data/uploads/**` are gitignored (folder kept via `.gitkeep`).

## Troubleshooting

### Cannot open database / login failed

- Confirm SQL Server/default instance/LocalDB is running.
- Confirm the Windows user can connect with integrated security.
- Create DB by enabling `Database:ApplyMigrations=true` in Development and starting the API.

### Transient SQL failures

EF Core is registered with `EnableRetryOnFailure`.

### Migration applied in Production by accident

Keep base `appsettings.json` with `ApplyMigrations=false` and `SeedData=false`. Enable only via explicit environment-specific config.

## Rules

- Prefer EF migrations over `EnsureCreated`.
- Never commit connection secrets.
- Preserve existing migrations and data; prefer additive schema changes.
