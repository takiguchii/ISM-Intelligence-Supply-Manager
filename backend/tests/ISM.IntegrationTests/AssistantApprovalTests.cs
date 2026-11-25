using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ISM.Application.Agents;
using ISM.Application.DTOs.Assistant;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.DTOs.Restaurant;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Modules.Assistant.Enums;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;
using ISM.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ISM.IntegrationTests;

public sealed class AssistantApprovalTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly FoundationWebApplicationFactory _factory;

    public AssistantApprovalTests(FoundationWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PriceProposal_Approve_UpdatesDishOnceAndCreatesAudit()
    {
        const int restaurantId = 991;
        var dishId = await SeedPriceAlertAsync(restaurantId, 80m, 120m);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);

        var actionsResponse = await client.GetAsync("/api/assistant/actions?limit=10");
        actionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var actions = await actionsResponse.Content.ReadFromJsonAsync<List<AssistantActionResponse>>();
        var action = actions.Should().ContainSingle(a => a.EntityId == dishId).Subject;
        action.OriginalValue.Should().Be(80m);
        action.ProposedValue.Should().Be(120m);

        (await ReadDishPriceAsync(dishId)).Should().Be(80m);
        var approval = await client.PostAsync($"/api/assistant/actions/{action.Id}/approve", null);
        approval.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadDishPriceAsync(dishId)).Should().Be(120m);

        var secondApproval = await client.PostAsync($"/api/assistant/actions/{action.Id}/approve", null);
        secondApproval.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        db.AssistantActionAudits.Count(a => a.EntityId == dishId && a.Action == "Approved").Should().Be(1);
        db.AssistantProposals.Single(p => p.Id == action.Id).Status.Should().Be(AssistantProposalStatus.Approved);
    }

    [Fact]
    public async Task PriceProposal_Reject_DoesNotChangeDish_AndStaleReturns409()
    {
        const int rejectedRestaurant = 992;
        var rejectedDishId = await SeedPriceAlertAsync(rejectedRestaurant, 80m, 120m);
        using var rejectClient = CreateClient(IsmRoles.Manager, rejectedRestaurant);
        var rejectAction = (await (await rejectClient.GetAsync("/api/assistant/actions?limit=10")).Content.ReadFromJsonAsync<List<AssistantActionResponse>>())!.Single(a => a.EntityId == rejectedDishId);
        (await rejectClient.PostAsync($"/api/assistant/actions/{rejectAction.Id}/reject", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadDishPriceAsync(rejectedDishId)).Should().Be(80m);

        const int staleRestaurant = 993;
        var staleDishId = await SeedPriceAlertAsync(staleRestaurant, 80m, 120m);
        using var staleClient = CreateClient(IsmRoles.Manager, staleRestaurant);
        var staleAction = (await (await staleClient.GetAsync("/api/assistant/actions?limit=10")).Content.ReadFromJsonAsync<List<AssistantActionResponse>>())!.Single(a => a.EntityId == staleDishId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            var dish = await db.Dishes.FindAsync(staleDishId);
            dish!.Price = 90m;
            await db.SaveChangesAsync();
        }
        (await staleClient.PostAsync($"/api/assistant/actions/{staleAction.Id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadDishPriceAsync(staleDishId)).Should().Be(90m);
    }

    [Fact]
    public async Task PriceProposal_Expires_IsAudited_AndCannotBeApproved()
    {
        const int restaurantId = 9911;
        var dishId = await SeedPriceAlertAsync(restaurantId, 80m, 120m);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);
        var action = (await (await client.GetAsync("/api/assistant/actions?limit=10")).Content.ReadFromJsonAsync<List<AssistantActionResponse>>())!
            .Single(a => a.EntityId == dishId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            var proposal = await db.AssistantProposals.SingleAsync(p => p.Id == action.Id);
            proposal.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var refreshed = await client.GetAsync("/api/assistant/actions?limit=10");
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var expired = (await refreshed.Content.ReadFromJsonAsync<List<AssistantActionResponse>>())!.Single(a => a.Id == action.Id);
        expired.Status.Should().Be(AssistantProposalStatus.Expired);
        (await client.PostAsync($"/api/assistant/actions/{action.Id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadDishPriceAsync(dishId)).Should().Be(80m);

        using var auditScope = _factory.Services.CreateScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<IsmDbContext>();
        auditDb.AssistantActionAudits.Count(a => a.ProposalId == action.Id && a.Action == "Expired").Should().Be(1);
    }

    [Fact]
    public async Task PriceProposal_RevisionChange_IsStaleWithoutMutatingPrice()
    {
        const int restaurantId = 9912;
        var dishId = await SeedPriceAlertAsync(restaurantId, 80m, 120m);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);
        var action = (await (await client.GetAsync("/api/assistant/actions?limit=10")).Content.ReadFromJsonAsync<List<AssistantActionResponse>>())!
            .Single(a => a.EntityId == dishId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            var dish = await db.Dishes.IgnoreQueryFilters().SingleAsync(d => d.Id == dishId);
            dish.PriceRevision++;
            await db.SaveChangesAsync();
        }

        (await client.PostAsync($"/api/assistant/actions/{action.Id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadDishPriceAsync(dishId)).Should().Be(80m);
        using var auditScope = _factory.Services.CreateScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<IsmDbContext>();
        auditDb.AssistantProposals.Single(p => p.Id == action.Id).Status.Should().Be(AssistantProposalStatus.Stale);
    }

    [Theory]
    [InlineData(IsmRoles.Chef)]
    [InlineData(IsmRoles.Waiter)]
    public async Task Assistant_RequiresManagerRole(string role)
    {
        using var client = CreateClient(role, 991);
        (await client.GetAsync("/api/assistant/overview")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Assistant_StatusAndChat_WhenDisabled_ReturnHonestFallback()
    {
        using var client = CreateClient(IsmRoles.Manager, 991);
        var status = await client.GetFromJsonAsync<AssistantStatusResponse>("/api/assistant/status");
        status.Should().NotBeNull();
        status!.Enabled.Should().BeFalse();
        status.Model.Should().Be("gemma3:270m");

        var response = await client.PostAsJsonAsync("/api/assistant/chat", new { message = "Altere o preço automaticamente" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chat = await response.Content.ReadFromJsonAsync<AssistantChatResponse>();
        chat!.Mode.Should().Be("deterministic_fallback");
        chat.Warning.Should().Contain("nenhuma ação");
    }

    [Fact]
    public async Task Assistant_AgentConfig_IsTenantScoped_AndStatusUsesSavedModel()
    {
        const int restaurantId = 997;
        await SeedRestaurantAsync(restaurantId);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);

        var initial = await client.GetAsync("/api/me/restaurant/agent-config");
        initial.StatusCode.Should().Be(HttpStatusCode.OK);
        (await initial.Content.ReadFromJsonAsync<RestaurantAgentConfigDto>())!.Model.Should().Be("gemma3:270m");

        var update = await client.PutAsJsonAsync("/api/me/restaurant/agent-config", new
        {
            apiEndpoint = "http://127.0.0.1:11434",
            model = "integration-model"
        });
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var status = await client.GetFromJsonAsync<AssistantStatusResponse>("/api/assistant/status");
        status!.Model.Should().Be("integration-model");
    }

    [Fact]
    public async Task Assistant_FinancialQuestion_UsesAuthorizedFinanceSourceWithoutLlm()
    {
        using var client = CreateClient(IsmRoles.Manager, 9980);

        var response = await client.PostAsJsonAsync("/api/assistant/chat", new { message = "Qual foi meu faturamento este mês?" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chat = await response.Content.ReadFromJsonAsync<AssistantChatResponse>();
        chat!.Mode.Should().Be("deterministic");
        chat.Source.Should().Contain("finance.getPeriodSummary");
        chat.Message.Should().Contain("pedidos pagos");
        chat.ToolName.Should().Be("finance.getPeriodSummary");
        chat.Specialist.Should().Be("finance");
        chat.DecisionCode.Should().Be(AgentDecisionCode.InsufficientData);
        chat.Message.Should().ContainEquivalentOf("dados");
    }

    [Fact]
    public async Task AutomationPolicy_FromConversation_IsDraftUntilExplicitConfirmation_AndRunsDryRun()
    {
        const int restaurantId = 9986;
        using var client = CreateClient(IsmRoles.Manager, restaurantId);
        var draftResponse = await client.PostAsJsonAsync("/api/assistant/chat", new { message = "Crie uma automação diária para verificar meu estoque" });
        draftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var draftChat = await draftResponse.Content.ReadFromJsonAsync<AssistantChatResponse>();
        draftChat!.ToolName.Should().Be("assistant.proposeAutomation");
        draftChat.DecisionCode.Should().Be(AgentDecisionCode.NeedsApproval);

        var policies = await client.GetFromJsonAsync<List<AutomationPolicyResponse>>("/api/assistant/automations");
        var policy = policies.Should().ContainSingle().Subject;
        policy.Status.Should().Be(AutomationPolicyStatus.Draft);
        policy.Mode.Should().Be(AutomationMode.DryRun);

        (await client.PostAsJsonAsync($"/api/assistant/automations/{policy.Id}/confirm", new { confirm = false })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var confirmed = await client.PostAsJsonAsync($"/api/assistant/automations/{policy.Id}/confirm", new { confirm = true });
        confirmed.StatusCode.Should().Be(HttpStatusCode.OK);
        var active = await confirmed.Content.ReadFromJsonAsync<AutomationPolicyResponse>();
        active!.Status.Should().Be(AutomationPolicyStatus.Active);
        active.Mode.Should().Be(AutomationMode.DryRun);

        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IBackgroundTenantContext>().SetRestaurant(restaurantId);
            var runner = scope.ServiceProvider.GetRequiredService<IAutomationRunner>();
            (await runner.RunDueAsync(restaurantId)).Should().Be(1);
            (await runner.RunDueAsync(restaurantId)).Should().Be(0);
        }

        var runs = await client.GetFromJsonAsync<List<AutomationRunResponse>>($"/api/assistant/automations/{policy.Id}/runs");
        var run = runs.Should().NotBeNull().And.ContainSingle().Subject;
        run.Status.Should().Be(AutomationRunStatus.Simulated);
        run.Reason.Should().Contain("dry-run");

        (await client.PostAsync($"/api/assistant/automations/{policy.Id}/pause", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IBackgroundTenantContext>().SetRestaurant(restaurantId);
            (await scope.ServiceProvider.GetRequiredService<IAutomationRunner>().RunDueAsync(restaurantId)).Should().Be(0);
        }
        (await client.PostAsync($"/api/assistant/automations/{policy.Id}/revoke", null)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AutomationPolicy_RejectsUnsafeActions_AndIsolatesTenants()
    {
        using var client = CreateClient(IsmRoles.Manager, 9987);
        var unsafeResponse = await client.PostAsJsonAsync("/api/assistant/automations", new
        {
            name = "Comprar sem limite", specialist = "stock", actionType = "purchase", target = "todos", intervalMinutes = 60
        });
        unsafeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var invalidZone = await client.PostAsJsonAsync("/api/assistant/automations", new
        {
            name = "Fuso inválido", specialist = "stock", actionType = "scanAlerts", target = "restaurante",
            intervalMinutes = 60, timeZone = "Invalid/Zone"
        });
        invalidZone.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var response = await client.PostAsJsonAsync("/api/assistant/automations", new
        {
            name = "Estoque isolado", specialist = "stock", actionType = "scanAlerts", target = "restaurante", intervalMinutes = 60
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var policy = await response.Content.ReadFromJsonAsync<AutomationPolicyResponse>();
        using var otherClient = CreateClient(IsmRoles.Manager, 9988);
        (await otherClient.GetAsync($"/api/assistant/automations/{policy!.Id}/runs")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await otherClient.GetFromJsonAsync<List<AutomationPolicyResponse>>("/api/assistant/automations"))!.Should().BeEmpty();
    }

    [Fact]
    public async Task AutomationPolicy_ExpiresBeforeRun_AndIsNotScheduled()
    {
        const int restaurantId = 9989;
        using var client = CreateClient(IsmRoles.Manager, restaurantId);
        var response = await client.PostAsJsonAsync("/api/assistant/automations", new
        {
            name = "Expiração controlada", specialist = "stock", actionType = "scanAlerts", target = "restaurante",
            intervalMinutes = 60, expiresAtUtc = DateTime.UtcNow.AddHours(1)
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var policy = await response.Content.ReadFromJsonAsync<AutomationPolicyResponse>();
        (await client.PostAsJsonAsync($"/api/assistant/automations/{policy!.Id}/confirm", new { confirm = true })).StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            var stored = await db.AutomationPolicies.SingleAsync(item => item.Id == policy.Id);
            stored.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
            scope.ServiceProvider.GetRequiredService<IBackgroundTenantContext>().SetRestaurant(restaurantId);
            (await scope.ServiceProvider.GetRequiredService<IAutomationRunner>().RunDueAsync(restaurantId)).Should().Be(0);
        }

        var listed = (await client.GetFromJsonAsync<List<AutomationPolicyResponse>>("/api/assistant/automations"))!.Single(item => item.Id == policy.Id);
        listed.Status.Should().Be(AutomationPolicyStatus.Expired);
        (await client.GetFromJsonAsync<List<AutomationRunResponse>>($"/api/assistant/automations/{policy.Id}/runs"))!.Should().BeEmpty();
    }

    [Fact]
    public async Task Assistant_ContextualQuestion_DelegatesToSpecialistAndKeepsSafeFallback()
    {
        const int restaurantId = 9985;
        var alertId = await SeedStockAlertAsync(restaurantId, 1m, 4m);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);

        var thread = await client.PostAsJsonAsync("/api/assistant/threads", new { originAlertId = alertId });
        thread.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversation = await thread.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        var response = await client.PostAsJsonAsync($"/api/assistant/threads/{conversation!.Thread.Id}/messages", new { message = "Devo repor este produto agora?" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        var assistantMessage = result!.Messages.Last(message => message.Role == "assistant");
        assistantMessage.Source.Should().Contain("stock");
        assistantMessage.Content.Should().Contain("Dados");
    }

    [Fact]
    public async Task Assistant_ToolRegistry_ListsReadOnlyAllowlistedTools()
    {
        using var client = CreateClient(IsmRoles.Manager, 9981);

        var response = await client.GetAsync("/api/assistant/tools");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tools = await response.Content.ReadFromJsonAsync<List<AgentToolDescriptor>>();
        tools.Should().NotBeNullOrEmpty();
        tools!.Should().OnlyContain(tool => tool.Operation == AgentToolOperation.Read && !tool.RequiresApproval);
        tools!.Select(tool => tool.ToolName).Should().Contain(["stock.getProduct", "stock.getOverview", "finance.getRevenueSummary", "pricing.getDishCost", "pricing.getDishOverview", "orders.getLatest", "orders.getRecent", "assistant.getAlertContext"]);
    }

    [Fact]
    public async Task Assistant_ToolExecution_UsesTenantFromTokenAndRejectsPromptTenant()
    {
        const int restaurantA = 9982;
        const int restaurantB = 9983;
        var alertId = await SeedStockAlertAsync(restaurantA, 1m, 4m);
        int productId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            productId = (await db.SystemAlerts.IgnoreQueryFilters().SingleAsync(alert => alert.Id == alertId)).ReferenceEntityId!.Value;
        }

        using var client = CreateClient(IsmRoles.Manager, restaurantB);
        var threadResponse = await client.PostAsJsonAsync("/api/assistant/threads", new { specialistId = "stock" });
        var thread = await threadResponse.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        var crossTenant = await client.PostAsJsonAsync("/api/assistant/tools/execute", new
        {
            threadId = thread!.Thread.Id,
            toolName = "stock.getProduct",
            input = new { productId }
        });
        var denied = await crossTenant.Content.ReadFromJsonAsync<AgentToolResult>();
        denied!.Code.Should().Be(AgentDecisionCode.InsufficientData);
        denied.Data.Should().BeNull();

        var promptTenant = await client.PostAsJsonAsync("/api/assistant/tools/execute", new
        {
            threadId = thread.Thread.Id,
            toolName = "stock.getProduct",
            input = new { productId, restaurantId = restaurantA }
        });
        var promptDenied = await promptTenant.Content.ReadFromJsonAsync<AgentToolResult>();
        promptDenied!.Code.Should().Be(AgentDecisionCode.Denied);
        promptDenied.Message.Should().Contain("tenant");

        using var otherRestaurantClient = CreateClient(IsmRoles.Manager, restaurantA);
        var foreignThread = await otherRestaurantClient.PostAsJsonAsync("/api/assistant/tools/execute", new
        {
            threadId = thread.Thread.Id,
            toolName = "stock.getProduct",
            input = new { productId }
        });
        foreignThread.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Assistant_ToolExecution_ReturnsEvidenceAndCorrelationForAuthorizedRead()
    {
        const int restaurantId = 9984;
        var alertId = await SeedStockAlertAsync(restaurantId, 1m, 4m);
        int productId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            productId = (await db.SystemAlerts.IgnoreQueryFilters().SingleAsync(alert => alert.Id == alertId)).ReferenceEntityId!.Value;
        }

        using var client = CreateClient(IsmRoles.Manager, restaurantId);
        var threadResponse = await client.PostAsJsonAsync("/api/assistant/threads", new { specialistId = "stock" });
        var thread = await threadResponse.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        var response = await client.PostAsJsonAsync("/api/assistant/tools/execute", new
        {
            threadId = thread!.Thread.Id,
            specialist = "finance",
            toolName = "stock.getProduct",
            correlationId = "p1-test-correlation",
            input = new { productId }
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentToolResult>();
        result!.Code.Should().Be(AgentDecisionCode.CanAct);
        result.CorrelationId.Should().Be("p1-test-correlation");
        result.Evidence.Should().ContainSingle().Which.SourceType.Should().Be("Product");
        result.DurationMilliseconds.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Assistant_ToolExecution_RejectsToolFromAnotherSpecialist()
    {
        using var client = CreateClient(IsmRoles.Manager, 9984);
        var threadResponse = await client.PostAsJsonAsync("/api/assistant/threads", new { specialistId = "stock" });
        var thread = await threadResponse.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        var response = await client.PostAsJsonAsync("/api/assistant/tools/execute", new
        {
            threadId = thread!.Thread.Id,
            toolName = "finance.getRevenueSummary",
            input = new { }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentToolResult>();
        result!.Code.Should().Be(AgentDecisionCode.Denied);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Assistant_StockThread_RejectsFinancialQuestionWithoutReturningFinancialFacts()
    {
        using var client = CreateClient(IsmRoles.Manager, 9986);
        var created = await client.PostAsJsonAsync("/api/assistant/threads", new { specialistId = "stock" });
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var thread = await created.Content.ReadFromJsonAsync<AssistantConversationResponse>();

        var response = await client.PostAsJsonAsync($"/api/assistant/threads/{thread!.Thread.Id}/messages", new { message = "Qual foi o faturamento deste mês?" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversation = await response.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        var answer = conversation!.Messages.Last(message => message.Role == "assistant");
        answer.Content.Should().Contain("só pode consultar informações dessa área");
        answer.Content.Should().NotContain("R$");
        answer.Source.Should().Be("Política de escopo do ISM");
    }

    [Fact]
    public async Task Assistant_Actions_DoNotLeakAcrossTenants()
    {
        await SeedPriceAlertAsync(994, 80m, 120m);
        using var client = CreateClient(IsmRoles.Manager, 995);
        var response = await client.GetAsync("/api/assistant/actions?limit=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<AssistantActionResponse>>()).Should().BeEmpty();
    }

    [Fact]
    public async Task OperationalAlert_Review_MarksAlertReadAndAuditsWithoutPriceMutation()
    {
        const int restaurantId = 996;
        var alertId = await SeedOperationalAlertAsync(restaurantId);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);

        var actions = await (await client.GetAsync("/api/assistant/actions?limit=10")).Content.ReadFromJsonAsync<List<AssistantActionResponse>>();
        var action = actions!.Single(a => a.AlertId == alertId);
        action.ActionType.Should().Be("ReviewAlert");

        var response = await client.PostAsync($"/api/assistant/actions/{action.Id}/review", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        (await db.SystemAlerts.IgnoreQueryFilters().SingleAsync(a => a.Id == alertId)).IsRead.Should().BeTrue();
        db.AssistantActionAudits.Count(a => a.ProposalId == action.Id && a.Action == "Reviewed").Should().Be(1);
    }

    [Fact]
    public async Task ContextualThread_UsesAlertFacts_AndSurvivesReload()
    {
        const int restaurantId = 997;
        var alertId = await SeedStockAlertAsync(restaurantId, 2m, 8m);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);

        var created = await client.PostAsJsonAsync("/api/assistant/threads", new { originAlertId = alertId });
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversation = await created.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        conversation.Should().NotBeNull();
        conversation!.Thread.OriginAlertId.Should().Be(alertId);
        conversation.Messages.Single().Content.Should().Contain("2").And.Contain("8");

        var answer = await client.PostAsJsonAsync($"/api/assistant/threads/{conversation.Thread.Id}/messages", new { message = "Por que entrou em alerta?" });
        answer.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await answer.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        updated!.Messages.Last().Content.Should().Contain("abaixo do mínimo").And.Contain("2").And.Contain("8");

        var reloaded = await client.GetFromJsonAsync<AssistantConversationResponse>($"/api/assistant/threads/{conversation.Thread.Id}");
        reloaded!.Messages.Should().HaveCount(3);
        (await client.GetFromJsonAsync<List<AssistantThreadResponse>>("/api/assistant/threads")).Should().ContainSingle(t => t.Id == conversation.Thread.Id);
    }

    [Fact]
    public async Task Thread_PersistsSpecialistIdentity_AndRejectsCrossSpecialistIntent()
    {
        const int restaurantId = 9971;
        var alertId = await SeedStockAlertAsync(restaurantId, 2m, 8m);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);

        var created = await client.PostAsJsonAsync("/api/assistant/threads", new
        {
            originAlertId = alertId,
            specialistId = "stock",
            sourceView = "dashboard",
            subjectEntityType = "Product"
        });
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversation = await created.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        conversation!.Thread.SpecialistId.Should().Be("stock");
        conversation.Thread.SourceView.Should().Be("dashboard");
        conversation.Thread.SubjectEntityType.Should().Be("Product");

        var denied = await client.PostAsJsonAsync($"/api/assistant/threads/{conversation.Thread.Id}/messages", new { message = "Qual foi meu faturamento?" });
        denied.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await denied.Content.ReadFromJsonAsync<AssistantConversationResponse>();
        updated!.Messages.Last().Content.Should().Contain("especialista de estoque");
        updated.Thread.SpecialistId.Should().Be("stock");

        var reloaded = await client.GetFromJsonAsync<AssistantConversationResponse>($"/api/assistant/threads/{conversation.Thread.Id}");
        reloaded!.Thread.SpecialistId.Should().Be("stock");
    }

    [Fact]
    public async Task Thread_RejectsSpecialistThatDoesNotMatchAlert()
    {
        const int restaurantId = 9972;
        var alertId = await SeedStockAlertAsync(restaurantId, 1m, 4m);
        using var client = CreateClient(IsmRoles.Manager, restaurantId);

        var response = await client.PostAsJsonAsync("/api/assistant/threads", new { originAlertId = alertId, specialistId = "finance" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ContextualThread_DoesNotAcceptAlertFromAnotherRestaurant()
    {
        var alertId = await SeedStockAlertAsync(998, 1m, 4m);
        using var client = CreateClient(IsmRoles.Manager, 999);
        var response = await client.PostAsJsonAsync("/api/assistant/threads", new { originAlertId = alertId });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateClient(string role, int restaurantId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(role, restaurantId));
        return client;
    }

    private async Task SeedRestaurantAsync(int restaurantId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        if (await db.Restaurants.FindAsync(restaurantId) is null)
        {
            db.Restaurants.Add(new Restaurant { Id = restaurantId, Name = $"Assist {restaurantId}", CNPJ = $"{restaurantId:00000000000000}", IsPlanActive = true, Created = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
    }

    private async Task<int> SeedPriceAlertAsync(int restaurantId, decimal currentPrice, decimal suggestedPrice)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        if (await db.Restaurants.FindAsync(restaurantId) is null)
        {
            db.Restaurants.Add(new Restaurant { Id = restaurantId, Name = $"Assist {restaurantId}", CNPJ = $"{restaurantId:00000000000000}", IsPlanActive = true, Created = DateTime.UtcNow });
        }
        var dish = new Dish { RestaurantId = restaurantId, Name = $"Prato Assist {restaurantId}", CategoryId = 1, Price = currentPrice, Cost = 48m, PriceRevision = 0, IsActive = true };
        db.Dishes.Add(dish);
        await db.SaveChangesAsync();
        db.SystemAlerts.Add(new SystemAlert
        {
            RestaurantId = restaurantId, AlertType = SystemAlertType.DishCmvDefasagem, Severity = AlertSeverity.Warning,
            Title = $"Defasagem {dish.Name}", Message = "Custo alterado", ReferenceEntityType = "Dish", ReferenceEntityId = dish.Id,
            PayloadSerializedJson = JsonSerializer.Serialize(new { DishName = dish.Name, SuggestedPrice = suggestedPrice, CurrentCmvPercent = 60m, TargetMarginPercent = 60m }),
            GeneratedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return dish.Id;
    }

    private async Task<decimal> ReadDishPriceAsync(int dishId)
    {
        using var scope = _factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<IsmDbContext>().Dishes.IgnoreQueryFilters().SingleAsync(d => d.Id == dishId)).Price;
    }

    private async Task<int> SeedOperationalAlertAsync(int restaurantId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        if (await db.Restaurants.FindAsync(restaurantId) is null)
            db.Restaurants.Add(new Restaurant { Id = restaurantId, Name = $"Assist {restaurantId}", CNPJ = $"{restaurantId:00000000000000}", IsPlanActive = true, Created = DateTime.UtcNow });
        var alert = new SystemAlert
        {
            RestaurantId = restaurantId, AlertType = SystemAlertType.StockBelowMinimum, Severity = AlertSeverity.Warning,
            Title = "Estoque mínimo", Message = "Produto abaixo do mínimo", ReferenceEntityType = "Product", ReferenceEntityId = 123,
            PayloadSerializedJson = "{}", GeneratedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
        };
        db.SystemAlerts.Add(alert);
        await db.SaveChangesAsync();
        return alert.Id;
    }

    private async Task<int> SeedStockAlertAsync(int restaurantId, decimal current, decimal minimum)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        if (await db.Restaurants.FindAsync(restaurantId) is null)
            db.Restaurants.Add(new Restaurant { Id = restaurantId, Name = $"Assist {restaurantId}", CNPJ = $"{restaurantId:00000000000000}", IsPlanActive = true, Created = DateTime.UtcNow });
        var product = new Product { RestaurantId = restaurantId, Name = $"Queijo Assist {restaurantId}", Unit = "kg", CurrentQuantity = current, MinimumQuantity = minimum, MaximumQuantity = 20m, ReorderPoint = minimum, AverageCost = 42.5m, IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var alert = new SystemAlert
        {
            RestaurantId = restaurantId, AlertType = SystemAlertType.StockBelowMinimum, Severity = AlertSeverity.Warning,
            Title = $"Estoque abaixo do mínimo: {product.Name}", Message = $"Saldo atual {current:N2}kg. Mínimo definido: {minimum:N2}kg.",
            ReferenceEntityType = "Product", ReferenceEntityId = product.Id,
            PayloadSerializedJson = JsonSerializer.Serialize(new { ProductId = product.Id, product.Name, CurrentQuantity = current, MinimumQuantity = minimum, Unit = product.Unit }),
            GeneratedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
        };
        db.SystemAlerts.Add(alert);
        await db.SaveChangesAsync();
        return alert.Id;
    }
}
