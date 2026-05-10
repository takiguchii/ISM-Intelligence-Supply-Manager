# Foundation Architecture

- `ISM.API`: HTTP entrypoint, controllers, middleware and Swagger
- `ISM.Application`: use cases, DTOs and application services
- `ISM.Domain`: entities and repository contracts
- `ISM.Infrastructure`: EF Core, MySQL provider, repositories, DbContext and migrations
- `ISM.CrossCutting`: shared configuration objects

The repository is optimized for containerized development first, with hot reload in both backend and frontend.
