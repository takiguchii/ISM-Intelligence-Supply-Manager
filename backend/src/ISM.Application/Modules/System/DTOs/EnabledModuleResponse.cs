namespace ISM.Application.Modules.System.DTOs;

public sealed record EnabledModuleResponse(
    int Id,
    string Name,
    string Slug,
    string Description,
    int SortOrder);
