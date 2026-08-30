namespace ISM.Application.Interfaces.DataImport;

/// <summary>
/// Define uma categoria de dados reconhecível dentro de uma planilha
/// (Estoque, Fornecedores, Finanças, ...).
/// Para suportar uma nova categoria, implemente esta interface e registre no DI —
/// o CsvStructureAnalyzer detecta e separa os dados automaticamente.
/// </summary>
public interface IImportCategoryProfile
{
    /// <summary>Nome da categoria (ex: "estoque", "fornecedores", "financas").</summary>
    string Category { get; }

    /// <summary>Entidade alvo da importação (ex: "Product", "Supplier").</summary>
    string TargetEntity { get; }

    /// <summary>
    /// Pontua o quanto os cabeçalhos da planilha combinam com esta categoria.
    /// 0 = nenhum match; 1 = todos os aliases conhecidos presentes.
    /// </summary>
    double MatchScore(IReadOnlyCollection<string> headers);

    /// <summary>Mapeia as colunas da linha original para campos normalizados desta categoria.</summary>
    Dictionary<string, string?> MapRow(Dictionary<string, string> row);

    /// <summary>Colunas da planilha consumidas por esta categoria (para detectar colunas não reconhecidas).</summary>
    IReadOnlyCollection<string> GetConsumedColumns(IReadOnlyCollection<string> headers);

    /// <summary>Valida a linha mapeada. Retorna mensagem de erro quando a linha não pode ser importada.</summary>
    string? Validate(Dictionary<string, string?> mappedRow) => null;

    /// <summary>Score mínimo para a categoria ser considerada presente na planilha.</summary>
    const double MinMatchThreshold = 0.3;
}
