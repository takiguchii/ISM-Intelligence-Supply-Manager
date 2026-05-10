namespace ISM.CrossCutting.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "MySql";
    public string ConnectionString { get; set; } = string.Empty;
}
