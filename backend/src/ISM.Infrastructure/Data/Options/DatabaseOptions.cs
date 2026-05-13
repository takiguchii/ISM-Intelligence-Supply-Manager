namespace ISM.Infrastructure.Data.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "MySql";
    public string ConnectionString { get; set; } = string.Empty;
}
