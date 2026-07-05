# Schoolera

Schoolera is scaffolded as a clean architecture solution with a .NET 10 API, EF Core SQL Server persistence, Unit of Work, and an Angular 22 frontend.

## Structure

- `src/Schoolera.Api` - ASP.NET Core API surface.
- `src/Schoolera.Application` - CQRS requests, handlers, validators, DTOs, and application contracts.
- `src/Schoolera.Domain` - Domain entities and business rules.
- `src/Schoolera.Infrastructure` - EF Core DbContext, repositories, migrations, and Unit of Work implementation.
- `tests/Schoolera.Tests` - Architecture and middleware validation tests.
- `frontend/schoolera-web` - Angular client using the `se` selector prefix.

## Validation Commands

```powershell
dotnet test Schoolera.slnx
cd frontend/schoolera-web
npm run build
ng test --watch=false
```

## Database Commands

The local EF tool is pinned in `dotnet-tools.json`.

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj --startup-project src/Schoolera.Api/Schoolera.Api.csproj
```

To add another migration:

```powershell
dotnet tool run dotnet-ef migrations add MigrationName --project src/Schoolera.Infrastructure/Schoolera.Infrastructure.csproj --startup-project src/Schoolera.Api/Schoolera.Api.csproj --output-dir Persistence/Migrations
```

## Local Run

```powershell
dotnet run --project src/Schoolera.Api/Schoolera.Api.csproj --urls http://127.0.0.1:5085
cd frontend/schoolera-web
npm start -- --port 4201 --host 127.0.0.1
```