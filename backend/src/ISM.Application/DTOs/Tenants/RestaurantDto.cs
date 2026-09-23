using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed class RestaurantDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome não pode exceder 120 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CNPJ é obrigatório.")]
    [StringLength(14, MinimumLength = 14, ErrorMessage = "O CNPJ deve conter 14 caracteres.")]
    public string CNPJ { get; set; } = string.Empty;
}