using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs.DataImport;

public sealed record SupplierCsvRowDto(
    [Required] [MaxLength(150)] string Name,
    [Required] [MaxLength(100)] string Category,
    [MaxLength(500)] string? Description,
    [EmailAddress] [MaxLength(150)] string? Email,
    [MaxLength(20)] string? Phone);