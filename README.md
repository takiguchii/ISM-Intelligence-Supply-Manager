# ISM - Intelligence Supply Manager

Foundation repository for the first architectural phase of the ISM vertical SaaS platform for restaurants.

## Stack

- Backend: .NET 8, ASP.NET Core, Entity Framework Core, Pomelo MySQL provider
- Frontend: Nuxt 3, Vue 3, Tailwind CSS, SCSS, Element Plus
- Database: MySQL
- Infrastructure: Docker, Docker Compose, hot reload with bind mounts

## Quick Start

1. Copy `.env.example` to `.env`.
2. Run `docker compose up --build`.
3. Open `http://localhost:3000`.
4. Open `http://localhost:8080/swagger`.

## Backend CLI

- Install the global EF tool:
  - `dotnet tool install --global dotnet-ef`
- Add dependencies:
  - `dotnet add package <Library>`
- Create a migration:
  - `dotnet ef migrations add <MigrationName> --project src/ISM.Infrastructure --startup-project src/ISM.API`
- Apply migrations:
  - `dotnet ef database update --project src/ISM.Infrastructure --startup-project src/ISM.API`
- Scaffold from an existing database:
  - `dotnet ef dbcontext scaffold "<connection-string>" Pomelo.EntityFrameworkCore.MySql --project src/ISM.Infrastructure --startup-project src/ISM.API`

## Project Structure

```txt
backend/      .NET 8 solution, clean architecture layers and tests
frontend/     Nuxt 3 application and temporary landing page
database/     SQL helpers, seeds and container init assets
docker/       Dockerfiles and entrypoints
docs/         Architecture, development and decision records
devops/       Operational helpers and compose notes
tests/        Repository-level smoke and container validation notes
```
