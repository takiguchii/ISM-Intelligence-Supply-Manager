using System.ComponentModel.DataAnnotations;
using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Menu.Entities;

public sealed class Dish : BaseEntity
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public int RestaurantId { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; } 
    public decimal Cost { get; set; }
    public bool IsActive { get; set; } = true;
    public bool Highlight { get; set; } = false;
    public string? UrlImage { get; set; }
    public int DisplayOrder { get; set; }
    
    public Restaurant? Restaurant { get; set; } 
    public Category? Category { get; set; }
    public ICollection<DishIngredient> Ingredients { get; set; } = new List<DishIngredient>(); //lista de ingredientes

}