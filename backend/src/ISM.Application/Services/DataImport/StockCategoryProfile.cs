namespace ISM.Application.Services.DataImport;

/// <summary>Categoria "estoque": mapeia colunas da planilha para campos de Product.</summary>
public sealed class StockCategoryProfile : ImportCategoryProfileBase
{
    public override string Category => "estoque";
    public override string TargetEntity => "Product";

    protected override (string Field, string[] Aliases)[] KnownFields =>
    [
        ("nome", ["Nome", "Name", "Produto", "Item"]),
        ("unidade", ["Unidade", "Unit", "Un", "UM"]),
        ("quantidadeAtual", ["QuantidadeAtual", "CurrentQuantity", "Qtd Atual", "Estoque", "EstoqueAtual", "Quantidade"]),
        ("quantidadeMinima", ["QuantidadeMinima", "MinimumQuantity", "Qtd Mínima", "Qtd Minima", "EstoqueMinimo", "Min"]),
        ("custoMedio", ["CustoMedio", "AverageCost", "Custo Médio", "Custo Medio", "PrecoCusto", "Preço Custo", "Custo"])
    ];

    public override string? Validate(Dictionary<string, string?> mappedRow)
    {
        if (string.IsNullOrWhiteSpace(mappedRow["nome"]))
            return "Campo 'nome' do produto é obrigatório.";
        if (!string.IsNullOrWhiteSpace(mappedRow["quantidadeAtual"]) &&
            !decimal.TryParse(mappedRow["quantidadeAtual"], out _))
            return $"Quantidade atual inválida: '{mappedRow["quantidadeAtual"]}'.";
        return null;
    }
}
