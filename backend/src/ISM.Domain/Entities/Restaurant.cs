using System.ComponentModel.DataAnnotations;
using ISM.Domain.Modules.Menu.Entities;

namespace ISM.Domain.Entities;

public sealed class Restaurant : BaseEntity
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string CNPJ { get; set; }
    public DateTime Created { get; set; }
    
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Dish> Dishes { get; set; } = [];
}