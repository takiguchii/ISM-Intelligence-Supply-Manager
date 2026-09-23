using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed class GenerateDevTokenRequest
{
    public int? UserId { get; set; }

    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string? Email { get; set; }

    public string? Role { get; set; }

    public int? RestaurantId { get; set; }
}
