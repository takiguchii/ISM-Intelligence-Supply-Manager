using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs.DataImport;

public sealed record SupplierCsvRowDto(
    [property: Required] [property: MaxLength(150)] string Name,
    [property: Required] [property: MaxLength(100)] string Category,
    [property: MaxLength(500)] string? Description,
    [property: EmailAddress] [property: MaxLength(150)] string? Email,
    [property: MaxLength(20)] string? Phone);
