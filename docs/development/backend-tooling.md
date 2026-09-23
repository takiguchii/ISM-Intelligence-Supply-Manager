# Backend Tooling

## Official NuGet Packages

- `Pomelo.EntityFrameworkCore.MySql` `8.0.3`
- `Microsoft.EntityFrameworkCore.Tools` `8.0.11`
- `Microsoft.EntityFrameworkCore.Design` `8.0.11`

`Pomelo.EntityFrameworkCore.MySql.Design` `1.1.2` is documented only as a legacy or evaluative package and is not part of the default `.NET 8 / EF Core 8` installation baseline.

## Official CLI Flow

Ao executar comandos da ferramenta `dotnet-ef` na raiz do projeto ou na pasta `backend/`, sempre informe os caminhos de `--project` (`ISM.Infrastructure`) e `--startup-project` (`ISM.API`):

```bash
# 1. Instalar a CLI do EF Core (versão alinhada com .NET 8)
dotnet tool install --global dotnet-ef --version 8.0.13

# 2. Adicionar uma nova Migration (na pasta backend/)
dotnet ef migrations add <NomeDaMigration> --project src/ISM.Infrastructure --startup-project src/ISM.API

# 3. Aplicar migrations no Banco de Dados MySQL (na pasta backend/)
dotnet ef database update --project src/ISM.Infrastructure --startup-project src/ISM.API

# 4. Remover última migration não aplicada
dotnet ef migrations remove --project src/ISM.Infrastructure --startup-project src/ISM.API

# 5. Executar via Docker Compose (opcional)
docker compose --profile tools run --rm db-migrate
```

## IDE Flow

- **Rider / Visual Studio:**
  - Projeto Padrão (Default Project): `ISM.Infrastructure`
  - Startup Project: `ISM.API`
  - Package Manager Console equivalents:
    - `Add-Migration <NomeDaMigration>`
    - `Update-Database`

