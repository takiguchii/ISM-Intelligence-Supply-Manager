using ISM.Domain.Modules.Stock.Entities;

namespace ISM.Domain.Modules.Menu.Entities;

public class DishIngredient 
{
    public int DishId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    
    public Dish? Dish { get; set; } 
    public Product? Product { get; set; } 
}