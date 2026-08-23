using System.ComponentModel.DataAnnotations;

namespace ISM.Domain.Entities;

public sealed class Plan : BaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public int MaxUsers { get; set; }
    public int MaxDishes { get; set; }
    public int MaxProducts { get; set; }
    public int MaxCategories { get; set; }

    public decimal MonthlyPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
}
