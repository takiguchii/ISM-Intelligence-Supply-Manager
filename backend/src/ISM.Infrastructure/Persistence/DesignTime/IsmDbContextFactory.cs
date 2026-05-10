using ISM.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ISM.Infrastructure.Persistence.DesignTime;

public sealed class IsmDbContextFactory : IDesignTimeDbContextFactory<IsmDbContext>
{
    public IsmDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Port=3306;Database=ism;User=ism;Password=ism;";

        var optionsBuilder = new DbContextOptionsBuilder<IsmDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 4, 0)),
            mySqlOptions => mySqlOptions.MigrationsAssembly(typeof(IsmDbContext).Assembly.FullName));

        return new IsmDbContext(optionsBuilder.Options);
    }
}
