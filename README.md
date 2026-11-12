# Refugio

ASP.NET Core (.NET 10) API organized in Clean Architecture layers, backed by PostgreSQL.

## Structure

```
src/
  Refugio.Api/             Entry point: Program.cs, Endpoints/, Filters/, Middlewares/, Extensions/
  Refugio.Application/     Use cases and contracts: UseCases/, DTOs/, Validators/, Interfaces/
  Refugio.Domain/          Business core: Entities/, Enums/, Exceptions/
  Refugio.Infrastructure/  Data/RefugioDbContext.cs, Mappings/, Repositories/, Auth/, External/ViaCep/
tests/
  Refugio.UnitTests/         Tests for Domain and Application
  Refugio.IntegrationTests/  End-to-end HTTP tests via WebApplicationFactory
```

### Layer dependencies

```
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure
```

- **Domain** has no project references.
- **Application** depends only on Domain. It declares interfaces (repositories, external services) in `Interfaces/`.
- **Infrastructure** implements those interfaces (EF Core, ViaCep, auth). It is registered through `AddInfrastructure()` in `DependencyInjection.cs`.
- **Api** composes everything in `Program.cs`.

EF Core entity configurations (`IEntityTypeConfiguration<T>`) placed in `Infrastructure/Mappings/` are applied automatically by `RefugioDbContext`.

## Prerequisites

- .NET SDK 10
- Docker (for PostgreSQL)

## Getting started

```bash
cp .env.example .env          # adjust values if needed
docker compose up -d          # starts PostgreSQL on localhost:5432
dotnet run --project src/Refugio.Api
```

Health check: `GET http://localhost:5159/health`
OpenAPI document (Development): `GET http://localhost:5159/openapi/v1.json`

The API reads its connection string from `ConnectionStrings:Refugio` (in `appsettings.Development.json` for local runs, or the `ConnectionStrings__Refugio` environment variable elsewhere).

## Tests

```bash
dotnet test
```

## Migrations

```bash
dotnet tool install --global dotnet-ef --version 10.*

dotnet ef migrations add InitialCreate \
  --project src/Refugio.Infrastructure \
  --startup-project src/Refugio.Api \
  --output-dir Data/Migrations

dotnet ef database update \
  --project src/Refugio.Infrastructure \
  --startup-project src/Refugio.Api
```
