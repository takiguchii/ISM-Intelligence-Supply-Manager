# Foundation Architecture

- `ISM.API`: HTTP entrypoint, controllers, middlewares, Swagger documentation, and host configuration
- `ISM.Application`: use cases, DTOs, application service interfaces (`Interfaces/`) and implementations (`Services/`)
- `ISM.Domain`: entities, repository contracts (`Interfaces/`), domain exceptions and business rules
- `ISM.Infrastructure`: EF Core, MySQL provider, DbContext (`IsmDbContext`), repository implementations, and migrations

The repository is optimized for containerized development first, with hot reload in both backend and frontend.

