namespace ISM.Application.Services.DataImport;

/// <summary>
/// Categoria "financas": mapeia colunas da planilha para campos financeiros
/// (lançamentos de receita/despesa). Estrutura de confirmação — persistência
/// em entidade própria (ex: SalesOrder/AccountsPayable) fica para quando o
/// módulo Finance existir.
/// </summary>
public sealed class FinanceCategoryProfile : ImportCategoryProfileBase
{
    public override string Category => "financas";
    public override string TargetEntity => "SalesOrder";

    protected override (string Field, string[] Aliases)[] KnownFields =>
    [
        ("data", ["Data", "Date", "DataPagamento", "DataVencimento", "DataPedido"]),
        ("descricao", ["Descricao", "Description", "Descrição", "Historico", "Histórico", "Lancamento"]),
        ("tipo", ["Tipo", "Type", "Natureza", "Categoria"]),
        ("valor", ["Valor", "Value", "Total", "ValorTotal", "Preco", "Preço", "Montante"]),
        ("formaPagamento", ["FormaPagamento", "Pagamento", "Payment", "MetodoPagamento"])
    ];

    public override string? Validate(Dictionary<string, string?> mappedRow)
    {
        if (!string.IsNullOrWhiteSpace(mappedRow["data"]) &&
            !DateTime.TryParse(mappedRow["data"], out _))
            return $"Data inválida: '{mappedRow["data"]}'.";
        if (!string.IsNullOrWhiteSpace(mappedRow["valor"]) &&
            !decimal.TryParse(mappedRow["valor"], out _))
            return $"Valor inválido: '{mappedRow["valor"]}'.";
        return null;
    }
}
