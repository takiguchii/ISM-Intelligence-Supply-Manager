using ISM.Application.Agents;
using ISM.Application.DTOs.Assistant;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Assistant;

[ApiController]
[Route("api/assistant")]
[Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
public sealed class AssistantController(IAssistantService service, IAgentToolRegistry tools, IAutomationService automations) : ControllerBase
{
    [HttpGet("status")]
    public Task<AssistantStatusResponse> Status(CancellationToken cancellationToken) => service.GetStatusAsync(cancellationToken);

    [HttpGet("overview")]
    public Task<AssistantOverviewResponse> Overview([FromQuery] int page = 1, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        => service.GetOverviewAsync(page, limit, cancellationToken);

    [HttpGet("actions")]
    public Task<IReadOnlyList<AssistantActionResponse>> Actions([FromQuery] int page = 1, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        => service.GetActionsAsync(page, limit, cancellationToken);

    [HttpGet("actions/{id:int}")]
    public async Task<ActionResult<AssistantActionResponse>> Action(int id, CancellationToken cancellationToken)
        => (await service.GetActionAsync(id, cancellationToken)) is { } action ? Ok(action) : NotFound();

    [HttpPost("actions/{id:int}/approve")]
    public async Task<ActionResult<AssistantActionResponse>> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await service.ApproveAsync(id, cancellationToken);
        return result.StatusCode switch { 200 => Ok(result.Action), 404 => NotFound(), 409 => Conflict(result.Action), _ => StatusCode(result.StatusCode) };
    }

    [HttpPost("actions/{id:int}/reject")]
    public async Task<ActionResult<AssistantActionResponse>> Reject(int id, CancellationToken cancellationToken)
        => (await service.RejectAsync(id, cancellationToken)) is { } action ? Ok(action) : NotFound();

    [HttpPost("actions/{id:int}/review")]
    public async Task<ActionResult<AssistantActionResponse>> Review(int id, CancellationToken cancellationToken)
        => (await service.ReviewAsync(id, cancellationToken)) is { } action ? Ok(action) : NotFound();

    [HttpGet("automations")]
    public async Task<ActionResult<IReadOnlyList<AutomationPolicyResponse>>> Automations(CancellationToken cancellationToken)
        => Ok(await automations.ListAsync(cancellationToken));

    [HttpPost("automations")]
    public async Task<ActionResult<AutomationPolicyResponse>> CreateAutomation([FromBody] CreateAutomationPolicyRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var policy = await automations.CreateAsync(request, cancellationToken);
        return policy is null ? BadRequest(new { message = "Especialista ou ação não permitida para automação." }) : CreatedAtAction(nameof(Automations), policy);
    }

    [HttpPost("automations/{id:int}/confirm")]
    public async Task<ActionResult<AutomationPolicyResponse>> ConfirmAutomation(int id, [FromBody] ConfirmAutomationRequest request, CancellationToken cancellationToken)
    {
        var result = await automations.ConfirmAsync(id, request, cancellationToken);
        return result.StatusCode switch { 200 => Ok(result.Policy), 400 => BadRequest(), 404 => NotFound(), 409 => Conflict(result.Policy), _ => StatusCode(result.StatusCode) };
    }

    [HttpPost("automations/{id:int}/pause")]
    public async Task<ActionResult<AutomationPolicyResponse>> PauseAutomation(int id, CancellationToken cancellationToken)
        => (await automations.PauseAsync(id, cancellationToken)) is { } policy ? Ok(policy) : NotFound();

    [HttpPost("automations/{id:int}/revoke")]
    public async Task<ActionResult<AutomationPolicyResponse>> RevokeAutomation(int id, CancellationToken cancellationToken)
        => (await automations.RevokeAsync(id, cancellationToken)) is { } policy ? Ok(policy) : NotFound();

    [HttpGet("automations/{id:int}/runs")]
    public async Task<ActionResult<IReadOnlyList<AutomationRunResponse>>> AutomationRuns(int id, CancellationToken cancellationToken)
        => Ok(await automations.RunsAsync(id, cancellationToken));

    [HttpPost("chat")]
    public async Task<ActionResult<AssistantChatResponse>> Chat([FromBody] AssistantChatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Ok(await service.ChatAsync(request.Message, cancellationToken));
    }

    [HttpGet("tools")]
    public ActionResult<IReadOnlyCollection<AgentToolDescriptor>> Tools() => Ok(tools.List());

    [HttpPost("tools/execute")]
    public async Task<ActionResult<AgentToolResult>> ExecuteTool([FromBody] AgentToolExecuteRequest request, CancellationToken cancellationToken)
        => Ok(await tools.ExecuteAsync(request, cancellationToken));

    [HttpGet("threads")]
    public Task<IReadOnlyList<AssistantThreadResponse>> Threads([FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        => service.GetThreadsAsync(page, limit, cancellationToken);

    [HttpPost("threads")]
    public async Task<ActionResult<AssistantConversationResponse>> CreateThread([FromBody] AssistantCreateThreadRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CreateThreadAsync(request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("threads/{id:int}")]
    public async Task<ActionResult<AssistantConversationResponse>> Thread(int id, CancellationToken cancellationToken)
        => (await service.GetThreadAsync(id, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [HttpGet("alerts/{id:int}/context")]
    public async Task<ActionResult<AssistantContextResponse>> AlertContext(int id, CancellationToken cancellationToken)
        => (await service.GetAlertContextAsync(id, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [HttpPost("threads/{id:int}/messages")]
    public async Task<ActionResult<AssistantConversationResponse>> SendMessage(int id, [FromBody] AssistantSendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.SendMessageAsync(id, request.Message, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
