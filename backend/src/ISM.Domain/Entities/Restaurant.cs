using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Stock.Entities;

namespace ISM.Domain.Entities;

public sealed class Restaurant : BaseEntity
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string CNPJ { get; set; } = string.Empty;
    public DateTime Created { get; set; }

    public int? PlanoId { get; set; }
    [ForeignKey(nameof(PlanoId))]
    public Plano? Plano { get; set; }

    public DateTime? TrialEndAtUtc { get; set; }
    public bool PlanoAtivo { get; set; } = true;

    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Dish> Dishes { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
}