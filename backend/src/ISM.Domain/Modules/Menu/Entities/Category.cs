using System.ComponentModel.DataAnnotations;
using ISM.Domain.Entities;
using ISM.Domain.Modules.Stock.Entities;

namespace ISM.Domain.Modules.Menu.Entities;

public sealed class Category : BaseEntity
{
    [Key]
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } =  true; 
    public int  DisplayOrder { get; set; }
    public ICollection<Dish> Dishes { get; set; } = new List<Dish>(); 
    
    public Restaurant? Restaurant { get; set; } 
}