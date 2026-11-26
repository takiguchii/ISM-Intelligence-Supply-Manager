using System.Diagnostics;
using System.Text.Json;
using ISM.Application.Agents;
using ISM.Application.Security;
using Microsoft.Extensions.Logging;

namespace ISM.Application.Services.Agents;

public sealed class AgentToolRegistry(IEnumerable<IAgentTool> tools, ICurrentUser currentUser, ILogger<AgentToolRegistry> logger) : IAgentToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAgentTool> _tools = tools.ToDictionary(
        tool => tool.Descriptor.ToolName,
        StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<AgentToolDescriptor> List() => _tools.Values.Select(tool => tool.Descriptor).ToArray();

    public async Task<AgentToolResult> ExecuteAsync(AgentToolExecuteRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId.Trim();
        var stopwatch = Stopwatch.StartNew();
        if (!currentUser.IsManagerOrAbove || currentUser.RestaurantId is not > 0)
            return Result(request.ToolName, AgentDecisionCode.Denied, "Usuário sem escopo gerencial de restaurante.", correlationId, stopwatch);
        if (string.IsNullOrWhiteSpace(request.ToolName) || !_tools.TryGetValue(request.ToolName.Trim(), out var tool))
            return Result(request.ToolName, AgentDecisionCode.InsufficientData, "Ferramenta não encontrada no registry allowlistado.", correlationId, stopwatch);
        if (!string.Equals(request.Specialist, tool.Descriptor.Specialist, StringComparison.OrdinalIgnoreCase))
            return Result(tool.Descriptor.ToolName, AgentDecisionCode.Denied, "A ferramenta não pertence à especialidade informada.", correlationId, stopwatch);
        if (request.Input.ValueKind is not JsonValueKind.Object)
            return Result(tool.Descriptor.ToolName, AgentDecisionCode.InsufficientData, "A entrada da ferramenta deve ser um objeto JSON.", correlationId, stopwatch);
        if (request.Input.TryGetProperty("restaurantId", out _))
            return Result(tool.Descriptor.ToolName, AgentDecisionCode.Denied, "O tenant é obtido da identidade autenticada e não pode vir da entrada.", correlationId, stopwatch);
        if (!tool.Descriptor.RequiredRoles.Contains(currentUser.Role, StringComparer.OrdinalIgnoreCase))
            return Result(tool.Descriptor.ToolName, AgentDecisionCode.Denied, "Papel não autorizado para esta ferramenta.", correlationId, stopwatch);

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Math.Clamp(tool.Descriptor.DeadlineMilliseconds, 100, 30_000));
            var result = await tool.ExecuteAsync(request.Input, new AgentToolContext(currentUser.UserId, currentUser.RestaurantId.Value, currentUser.Role, correlationId), timeout.Token);
            return result with { DurationMilliseconds = stopwatch.ElapsedMilliseconds };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result(tool.Descriptor.ToolName, AgentDecisionCode.Unreachable, "A ferramenta excedeu o prazo de execução.", correlationId, stopwatch);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha na ferramenta {ToolName} para correlação {CorrelationId}", tool.Descriptor.ToolName, correlationId);
            return Result(tool.Descriptor.ToolName, AgentDecisionCode.Unreachable, "A ferramenta não pôde consultar a fonte autorizada.", correlationId, stopwatch);
        }
    }

    private static AgentToolResult Result(string? toolName, AgentDecisionCode code, string message, string correlationId, Stopwatch stopwatch)
        => new(toolName ?? string.Empty, code, null, [], message, correlationId, stopwatch.ElapsedMilliseconds);
}
