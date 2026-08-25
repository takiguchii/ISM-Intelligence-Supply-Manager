using ISM.Application.Interfaces.DataImport;

namespace ISM.Application.Services.DataImport;

/// <summary>
/// Base de perfis de categoria: pontuação por aliases de cabeçalho,
/// mapeamento de linhas e detecção de colunas consumidas.
/// A subclasse declara Category/TargetEntity/KnownFields e (opcionalmente) Validate.
/// </summary>
public abstract class ImportCategoryProfileBase : IImportCategoryProfile
{
    public abstract string Category { get; }
    public abstract string TargetEntity { get; }

    /// <summary>Campos normalizados da categoria e seus aliases de cabeçalho.</summary>
    protected abstract (string Field, string[] Aliases)[] KnownFields { get; }

    public double MatchScore(IReadOnlyCollection<string> headers)
    {
        var totalAliases = KnownFields.Sum(f => f.Aliases.Length);
        var matched = CountMatchedHeaders(headers);
        return totalAliases == 0 ? 0 : (double)matched / totalAliases;
    }

    public Dictionary<string, string?> MapRow(Dictionary<string, string> row)
    {
        var mapped = new Dictionary<string, string?>();
        foreach (var (field, aliases) in KnownFields)
            mapped[field] = FirstValue(row, aliases);
        return mapped;
    }

    public IReadOnlyCollection<string> GetConsumedColumns(IReadOnlyCollection<string> headers)
        => headers.Where(h =>
            KnownFields.Any(f => f.Aliases.Contains(h.Trim(), StringComparer.OrdinalIgnoreCase)))
            .ToList();

    public virtual string? Validate(Dictionary<string, string?> mappedRow) => null;

    private int CountMatchedHeaders(IReadOnlyCollection<string> headers)
        => KnownFields.Sum(f =>
            headers.Count(h => f.Aliases.Contains(h.Trim(), StringComparer.OrdinalIgnoreCase)));

    protected static string? FirstValue(Dictionary<string, string> row, string[] aliases)
    {
        foreach (var alias in aliases)
            if (row.TryGetValue(alias, out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        return null;
    }
}
