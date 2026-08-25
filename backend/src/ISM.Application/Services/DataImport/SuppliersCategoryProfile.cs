using System.Text.RegularExpressions;

namespace ISM.Application.Services.DataImport;

/// <summary>Categoria "fornecedores": mapeia colunas da planilha para campos de Supplier.</summary>
public sealed class SuppliersCategoryProfile : ImportCategoryProfileBase
{
    public override string Category => "fornecedores";
    public override string TargetEntity => "Supplier";

    protected override (string Field, string[] Aliases)[] KnownFields =>
    [
        ("nome", ["Nome", "Name", "Fornecedor", "Supplier"]),
        ("categoria", ["Categoria", "Category", "Ramo"]),
        ("descricao", ["Descricao", "Description", "Descrição", "Observacao", "Observação"]),
        ("email", ["Email", "E-mail", "EmailContato"]),
        ("telefone", ["Telefone", "Phone", "Tel", "Contato"])
    ];

    public override string? Validate(Dictionary<string, string?> mappedRow)
    {
        if (string.IsNullOrWhiteSpace(mappedRow["nome"]))
            return "Campo 'nome' do fornecedor é obrigatório.";
        var email = mappedRow["email"];
        if (!string.IsNullOrWhiteSpace(email) &&
            !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return $"Email inválido: '{email}'.";
        return null;
    }
}
