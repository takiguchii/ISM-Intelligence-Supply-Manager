using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record UpdateSupplierRequest(
    [Required]
    [MaxLength(150)]
    string Name,
    [Required]
    [MaxLength(100)]
    string Category,
    [MaxLength(500)]
    string? Description,
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    string Email,
    [Required]
    [MaxLength(20)]
    string Phone);
