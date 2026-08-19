using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed class RegisterTenantRequest
{
    [Required(ErrorMessage = "O nome do restaurante é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome do restaurante não pode exceder 120 caracteres.")]
    public string RestaurantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CNPJ do restaurante é obrigatório.")]
    [StringLength(14, MinimumLength = 14, ErrorMessage = "O CNPJ deve conter 14 caracteres (somente números).")]
    public string RestaurantCnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nome do usuário gerente é obrigatório.")]
    [StringLength(150, ErrorMessage = "O nome não pode exceder 150 caracteres.")]
    public string ManagerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail do gerente é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail em formato inválido.")]
    public string ManagerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha do gerente é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
    public string ManagerPassword { get; set; } = string.Empty;

    public int? PlanoId { get; set; }
}
