namespace ISM.Application.DTOs.DataImport;

/// <summary>
/// Resultado da análise (dry-run) de uma planilha: estrutura detectada,
/// dados separados por categoria e confirmação por categoria — sem gravar no banco.
/// </summary>
public sealed class ImportPreviewDto
{
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public int TotalRows { get; set; }
    public IReadOnlyList<string> Headers { get; set; } = [];
    public IReadOnlyList<string> UnmappedColumns { get; set; } = [];
    public IReadOnlyList<CategoryConfirmationDto> Categories { get; set; } = [];
}

/// <summary>
/// Confirmação dos dados separados por categoria (ex: Estoque, Fornecedores, Finanças).
/// </summary>
public sealed class CategoryConfirmationDto
{
    public string Category { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public int RowCount { get; set; }

    /// <summary>True quando todas as linhas passam na validação.</summary>
    public bool CanImport => Errors.Count == 0 && RowsWithErrors.Count == 0;

    /// <summary>Campos normalizados mapeados por linha (dry-run: nada é gravado).</summary>
    public IReadOnlyList<Dictionary<string, string?>> MappedFields { get; set; } = [];

    public IReadOnlyList<int> RowsWithErrors { get; set; } = [];

    public IReadOnlyList<string> Errors { get; set; } = [];
}
