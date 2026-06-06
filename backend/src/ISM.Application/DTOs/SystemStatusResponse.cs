namespace ISM.Application.DTOs;

public sealed record SystemStatusResponse(
    string ApplicationName,
    string Environment,
    DateTime UtcTimestamp,
    bool DatabaseConnected,
    int EnabledModuleCount,
    EnabledModuleResponse? FirstEnabledModule,
    IReadOnlyList<EnabledModuleResponse> EnabledModules);
