using System.ComponentModel.DataAnnotations;

namespace ISM.Domain.Entities;

public sealed class Plano : BaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Descricao { get; set; }

    public int MaxUsuarios { get; set; }
    public int MaxPratos { get; set; }
    public int MaxProdutos { get; set; }
    public int MaxCategorias { get; set; }

    public decimal PrecoMensal { get; set; }

    public bool Ativo { get; set; } = true;

    public ICollection<Restaurant> Restaurantes { get; set; } = new List<Restaurant>();
}
