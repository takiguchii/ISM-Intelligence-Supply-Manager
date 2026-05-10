# Backend Tooling

## Official NuGet Packages

- `Pomelo.EntityFrameworkCore.MySql` `8.0.3`
- `Microsoft.EntityFrameworkCore.Tools` `8.0.11`
- `Microsoft.EntityFrameworkCore.Design` `8.0.11`

`Pomelo.EntityFrameworkCore.MySql.Design` `1.1.2` is documented only as a legacy or evaluative package and is not part of the default `.NET 8 / EF Core 8` installation baseline.

## Official CLI Flow

```bash
dotnet tool install --global dotnet-ef
dotnet add package <Library>
dotnet ef dbcontext scaffold
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## IDE Flow

- Rider
- Visual Studio Community
- Package Manager Console equivalents:
  - `Add-Migration`
  - `Update-Database`
