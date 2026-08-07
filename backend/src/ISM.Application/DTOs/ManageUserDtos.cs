using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed class UpdateUserRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(255, ErrorMessage = "Nome deve ter no máximo 255 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [MaxLength(255, ErrorMessage = "E-mail deve ter no máximo 255 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role é obrigatória")]
    [MaxLength(50, ErrorMessage = "Role deve ter no máximo 50 caracteres")]
    public string Role { get; set; } = string.Empty;

    public int? RestaurantId { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class UserDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? RestaurantId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ToggleUserActiveRequest
{
    public bool IsActive { get; set; }
}
