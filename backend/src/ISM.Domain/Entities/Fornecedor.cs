using ISM.Domain.Entities;

namespace ISM.Domain.Entities;

public sealed class Fornecedor : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
}
