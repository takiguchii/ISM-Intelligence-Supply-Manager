namespace ISM.Application.DTOs;

public sealed record EnabledModuleResponse(
    int Id,
    string Name,
    string Slug,
    string Description,
    int SortOrder);
