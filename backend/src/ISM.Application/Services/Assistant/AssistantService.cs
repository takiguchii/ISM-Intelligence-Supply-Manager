using System.Globalization;
using System.Text.Json;
using ISM.Application.DTOs.Assistant;
using ISM.Application.Agents;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Interfaces.Finance;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Assistant.Entities;
using ISM.Domain.Modules.Assistant.Enums;
using ISM.Domain.Modules.System.Enums;

namespace ISM.Application.Services.Assistant;

public sealed class AssistantService(
    IAssistantRepository repository,
    IAssistantConversationRepository conversations,
    ISystemAlertRepository alerts,
    IProductRepository products,
    IDishRepository dishes,
    IFinanceService finance,
    ILlmTextProvider llm,
    ICurrentUser currentUser,
    IAgentToolRegistry tools,
    AssistantIntentCoordinator intentCoordinator,
    IAutomationService automations) : IAssistantService
{
    public async Task<AssistantStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = await llm.GetStatusAsync(cancellationToken);
        return new(status.Enabled, status.Reachable, status.Model,
            !status.Enabled ? "IA desabilitada; dados determinísticos continuam disponíveis."
            : status.Reachable ? $"{status.Provider} disponível" : $"{status.Provider} indisponível; dados determinísticos continuam disponíveis.");
    }

    public async Task<AssistantOverviewResponse> GetOverviewAsync(int page, int limit, CancellationToken cancellationToken = default)
    {
        var actions = await GetActionsAsync(page, limit, cancellationToken);
        var status = await GetStatusAsync(cancellationToken);
        var alertPage = await alerts.GetPagedAsync(RequireRestaurant(), 1, 1, isRead: false, isDismissed: false, cancellationToken: cancellationToken);
        return new(status, actions.Count(a => a.Status == AssistantProposalStatus.Pending), alertPage.TotalCount, actions);
    }

    public async Task<IReadOnlyList<AssistantActionResponse>> GetActionsAsync(int page, int limit, CancellationToken cancellationToken = default)
    {
        var restaurantId = RequireRestaurant();
        await repository.ExpirePendingAsync(restaurantId, DateTime.UtcNow, cancellationToken);
        var alertPage = await alerts.GetPagedAsync(restaurantId, 1, Math.Min(Math.Max(limit * 2, 20), 200), isDismissed: false, cancellationToken: cancellationToken);
        foreach (var alert in alertPage.Items)
        {
            if (await repository.FindByAlertAsync(alert.Id, restaurantId, cancellationToken) is not null) continue;
            var proposal = await BuildProposalAsync(alert, restaurantId, cancellationToken);
            if (proposal is not null) await repository.CreateProposalAsync(proposal, cancellationToken);
        }
        return (await repository.GetProposalsAsync(restaurantId, page, Math.Min(Math.Max(limit, 1), 50), cancellationToken)).Select(Map).ToList();
    }

    public async Task<AssistantActionResponse?> GetActionAsync(int id, CancellationToken cancellationToken = default)
        => (await repository.GetProposalAsync(id, RequireRestaurant(), cancellationToken)) is { } p ? Map(p) : null;

    public async Task<(AssistantActionResponse? Action, int StatusCode)> ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await repository.ApprovePriceAsync(id, RequireRestaurant(), currentUser.UserId, cancellationToken);
        if (result.Proposal is null) return (null, 404);
        if (result.IsStale || result.WasAlreadyDecided) return (Map(result.Proposal), 409);
        return (Map(result.Proposal), 200);
    }

    public async Task<AssistantActionResponse?> RejectAsync(int id, CancellationToken cancellationToken = default)
        => (await repository.RejectAsync(id, RequireRestaurant(), currentUser.UserId, cancellationToken)) is { } p ? Map(p) : null;

    public async Task<AssistantActionResponse?> ReviewAsync(int id, CancellationToken cancellationToken = default)
        => (await repository.ReviewAsync(id, RequireRestaurant(), currentUser.UserId, cancellationToken)) is { } p ? Map(p) : null;

    public async Task<AssistantChatResponse> ChatAsync(string message, CancellationToken cancellationToken = default)
    {
        var restaurantId = RequireRestaurant();
        var context = await BuildContextAsync(restaurantId, cancellationToken);
        if (await TryCreateAutomationDraftAsync(message, cancellationToken) is { } automationDraft)
            return automationDraft;
        var intent = intentCoordinator.Resolve(message, null);
        if (intent is not null && await ExecuteIntentAsync(intent, cancellationToken) is { } routed)
            return routed;
        var result = await llm.CompleteAsync(
            "Você é o ISM Assist. Responda em português, seja conciso e não invente dados. Nunca altere preços, estoque ou fornecedores; explique que qualquer alteração exige aprovação do gerente. Use somente o contexto fornecido.",
            $"Contexto autorizado do restaurante:\n{context}\n\nPergunta: {message.Trim()}", cancellationToken);
        return result.Success
            ? new("llm", result.Text!.Trim(), "Ollama local", null)
            : new("deterministic_fallback", "A conversação com o modelo local está indisponível no momento. Não tenho dados suficientes para concluir isso; revise os alertas determinísticos do ISM.", "Dados determinísticos do ISM", "Tente novamente quando o Ollama estiver disponível; nenhuma ação foi executada.");
    }

    public async Task<IReadOnlyList<AssistantThreadResponse>> GetThreadsAsync(int page, int limit, CancellationToken cancellationToken = default)
    {
        var restaurantId = RequireRestaurant();
        var threads = await conversations.GetThreadsAsync(restaurantId, currentUser.UserId, Math.Max(page, 1), Math.Clamp(limit, 1, 50), cancellationToken);
        return threads.Select(MapThread).ToList();
    }

    public async Task<AssistantConversationResponse?> CreateThreadAsync(AssistantCreateThreadRequest request, CancellationToken cancellationToken = default)
    {
        var restaurantId = RequireRestaurant();
        if (request.ParentThreadId.HasValue && await conversations.GetThreadAsync(request.ParentThreadId.Value, restaurantId, currentUser.UserId, cancellationToken) is null)
            return null;

        AssistantContextResponse? context = null;
        if (request.OriginAlertId.HasValue)
        {
            context = await GetAlertContextAsync(request.OriginAlertId.Value, cancellationToken);
            if (context is null) return null;
        }

        var now = DateTime.UtcNow;
        var thread = await conversations.CreateThreadAsync(new AssistantThread
        {
            RestaurantId = restaurantId,
            UserId = currentUser.UserId,
            ParentThreadId = request.ParentThreadId,
            OriginAlertId = request.OriginAlertId,
            OriginType = context is null ? "General" : "SystemAlert",
            SubjectEntityId = context?.ReferenceEntityId,
            Subject = Limit(request.Subject?.Trim(), 150) ?? context?.EntityName ?? "Conversa com o ISM Assist",
            ContextSnapshotJson = context is null ? "{}" : JsonSerializer.Serialize(context),
            LastMessageAtUtc = now,
            CreatedAtUtc = now
        }, cancellationToken);

        await conversations.AddMessageAsync(new AssistantMessage
        {
            RestaurantId = restaurantId,
            ThreadId = thread.Id,
            Role = "assistant",
            Content = context is null ? "Estou pronto para consultar os dados autorizados deste restaurante. O que você gostaria de analisar?" : BuildOpeningMessage(context),
            Mode = "deterministic",
            Source = "Dados determinísticos do ISM",
            CreatedAtUtc = now
        }, cancellationToken);

        return await BuildConversationAsync(thread, context, cancellationToken);
    }

    public async Task<AssistantConversationResponse?> GetThreadAsync(int threadId, CancellationToken cancellationToken = default)
    {
        var restaurantId = RequireRestaurant();
        var thread = await conversations.GetThreadAsync(threadId, restaurantId, currentUser.UserId, cancellationToken);
        return thread is null ? null : await BuildConversationAsync(thread, DeserializeContext(thread.ContextSnapshotJson), cancellationToken);
    }

    public async Task<AssistantContextResponse?> GetAlertContextAsync(int alertId, CancellationToken cancellationToken = default)
    {
        var restaurantId = RequireRestaurant();
        var alert = await alerts.GetByIdAsync(alertId, cancellationToken);
        return alert is null || alert.RestaurantId != restaurantId ? null : await BuildAlertContextAsync(alert, restaurantId, cancellationToken);
    }

    public async Task<AssistantConversationResponse?> SendMessageAsync(
        int threadId,
        string message,
        CancellationToken cancellationToken = default,
        byte[]? imageBytes = null,
        string? imageContentType = null,
        string? imageFileName = null)
    {
        var restaurantId = RequireRestaurant();
        var thread = await conversations.GetThreadAsync(threadId, restaurantId, currentUser.UserId, cancellationToken);
        var cleanMessage = message.Trim();
        var hasImage = imageBytes is { Length: > 0 };
        if (thread is null || (string.IsNullOrWhiteSpace(cleanMessage) && !hasImage)) return null;
        if (hasImage && imageContentType is not ("image/png" or "image/jpeg" or "image/webp"))
            throw new InvalidOperationException("O chat aceita imagens PNG, JPEG ou WEBP.");

        var promptMessage = string.IsNullOrWhiteSpace(cleanMessage)
            ? "Analise o anexo enviado e descreva os dados relevantes para a gestão do restaurante."
            : cleanMessage;
        var storedMessage = hasImage
            ? Limit($"{promptMessage}\n\n[Imagem anexada: {imageFileName ?? "arquivo"}]", 6000)!
            : Limit(promptMessage, 1000)!;

        await conversations.AddMessageAsync(new AssistantMessage
        {
            RestaurantId = restaurantId, ThreadId = thread.Id, UserId = currentUser.UserId, Role = "user",
            Content = storedMessage, Mode = "user", Source = "Administrador", CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        var context = DeserializeContext(thread.ContextSnapshotJson);
        if (!hasImage && await TryCreateAutomationDraftAsync(promptMessage, cancellationToken) is { } automationDraft)
        {
            await conversations.AddMessageAsync(new AssistantMessage
            {
                RestaurantId = restaurantId, ThreadId = thread.Id, Role = "assistant", Content = automationDraft.Message,
                Mode = automationDraft.Mode, Source = automationDraft.Source, CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
            thread.LastMessageAtUtc = DateTime.UtcNow;
            thread.UpdatedAtUtc = thread.LastMessageAtUtc;
            await conversations.SaveThreadAsync(thread, cancellationToken);
            return await BuildConversationAsync(thread, context, cancellationToken);
        }
        var intent = hasImage ? null : intentCoordinator.Resolve(promptMessage, context);
        if (intent is not null && await ExecuteIntentAsync(intent, cancellationToken) is { } routed)
        {
            await conversations.AddMessageAsync(new AssistantMessage
            {
                RestaurantId = restaurantId, ThreadId = thread.Id, Role = "assistant", Content = routed.Message,
                Mode = routed.Mode, Source = routed.Source, CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
            thread.LastMessageAtUtc = DateTime.UtcNow;
            thread.UpdatedAtUtc = thread.LastMessageAtUtc;
            await conversations.SaveThreadAsync(thread, cancellationToken);
            return await BuildConversationAsync(thread, context, cancellationToken);
        }
        if (!hasImage && IsFinancialQuestion(promptMessage))
        {
            var financial = await BuildFinancialResponseAsync(cancellationToken);
            await conversations.AddMessageAsync(new AssistantMessage
            {
                RestaurantId = restaurantId, ThreadId = thread.Id, Role = "assistant", Content = financial.Message,
                Mode = financial.Mode, Source = financial.Source, CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
            thread.LastMessageAtUtc = DateTime.UtcNow;
            thread.UpdatedAtUtc = thread.LastMessageAtUtc;
            await conversations.SaveThreadAsync(thread, cancellationToken);
            return await BuildConversationAsync(thread, context, cancellationToken);
        }
        var deterministic = hasImage ? null : BuildDeterministicResponse(promptMessage, context);
        AssistantChatResponse response;
        if (deterministic is not null)
        {
            response = new("deterministic", deterministic, "Dados determinísticos do ISM", null);
        }
        else
        {
            var factualContext = context is null ? await BuildContextAsync(restaurantId, cancellationToken) : JsonSerializer.Serialize(context);
            var history = await conversations.GetMessagesAsync(thread.Id, restaurantId, currentUser.UserId, 8, cancellationToken);
            var historyText = string.Join('\n', history.TakeLast(8).Select(item => $"{item.Role}: {item.Content}"));
            const string systemPrompt = "Você é o ISM Assist. Responda em português simples e conciso. Use somente os fatos fornecidos e o conteúdo visual do anexo. Não invente números, IDs ou histórico. Não execute nem autorize alterações; qualquer ação exige confirmação do administrador e as regras do ISM.";
            var userPrompt = $"Contexto factual autorizado:\n{factualContext}\n\nHistórico recente:\n{historyText}\n\nPergunta atual: {promptMessage}";
            var result = hasImage
                ? await llm.CompleteWithImageAsync(systemPrompt, userPrompt, imageBytes!, imageContentType!, cancellationToken)
                : await llm.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
            response = result.Success
                ? new("llm", Limit(result.Text!.Trim(), 5000)!, "Ollama local", null)
                : new("deterministic_fallback", "A conversação com o modelo local está indisponível no momento. Não tenho dados suficientes para concluir isso além dos fatos exibidos nesta conversa.", "Dados determinísticos do ISM", "Tente novamente quando o Ollama estiver disponível; nenhuma ação foi executada.");
        }

        await conversations.AddMessageAsync(new AssistantMessage
        {
            RestaurantId = restaurantId, ThreadId = thread.Id, Role = "assistant", Content = response.Message,
            Mode = response.Mode, Source = response.Source, CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
        thread.LastMessageAtUtc = DateTime.UtcNow;
        thread.UpdatedAtUtc = thread.LastMessageAtUtc;
        await conversations.SaveThreadAsync(thread, cancellationToken);
        return await BuildConversationAsync(thread, context, cancellationToken);
    }

    private int RequireRestaurant() => currentUser is { IsManagerOrAbove: true, RestaurantId: > 0 } ? currentUser.RestaurantId.Value : throw new UnauthorizedAccessException("Restaurante gerencial não identificado.");

    private async Task<AssistantChatResponse?> TryCreateAutomationDraftAsync(string message, CancellationToken cancellationToken)
    {
        var lower = message.Trim().ToLowerInvariant();
        var asksForPolicy = lower.Contains("automação") || lower.Contains("automacao") || lower.Contains("todo dia") || lower.Contains("diariamente") || lower.Contains("política") || lower.Contains("politica");
        if (!asksForPolicy) return null;
        if ((lower.Contains("confirm") || lower.Contains("ativar") || lower.Contains("ative")) && (lower.Contains("autom") || lower.Contains("política") || lower.Contains("politica")))
        {
            var draft = (await automations.ListAsync(cancellationToken)).LastOrDefault(policy => policy.Status == AutomationPolicyStatus.Draft);
            if (draft is null) return new("deterministic", "Não encontrei um rascunho de automação pendente para confirmar.", "Política de automação do ISM", "Nenhuma ação foi executada.");
            var confirmed = await automations.ConfirmAsync(draft.Id, new ConfirmAutomationRequest(true), cancellationToken);
            if (confirmed.Policy is null) return new("deterministic", "Não foi possível confirmar o rascunho; ele pode ter sido alterado ou expirado.", "Política de automação do ISM", "Nenhuma ação foi executada.");
            return new("deterministic", $"Política “{confirmed.Policy.Name}” confirmada em dry-run. Ela está ativa para análise, mas não altera estoque, preços, financeiro ou fornecedores.", "Política de automação do ISM", "Execuções serão simuladas e registradas no histórico.")
            {
                Specialist = "assistant", ToolName = "assistant.confirmAutomation", DecisionCode = confirmed.StatusCode == 200 ? AgentDecisionCode.CanAct : AgentDecisionCode.NeedsApproval
            };
        }
        if (lower.Contains("compr") || lower.Contains("pag") || lower.Contains("enviar") || lower.Contains("alter") || lower.Contains("mudar") || lower.Contains("mude") || lower.Contains("execut") || lower.Contains("fornecedor sem limite"))
            return new("deterministic", "Não vou ativar uma automação de compra, pagamento ou envio externo. Essa operação exige política específica, teto, validade e aprovação individual; nenhum rascunho executável foi criado.", "Política de automação do ISM", "Nenhuma ação foi executada.");

        var (specialist, action, label) = lower.Contains("preço") || lower.Contains("preco") || lower.Contains("cmv")
            ? ("pricing", "scanCmv", "revisão periódica de CMV")
            : lower.Contains("fornecedor") || lower.Contains("cotação") || lower.Contains("cotacao")
                ? ("suppliers", "scanOffers", "revisão periódica de ofertas")
                : lower.Contains("finance") || lower.Contains("faturamento") || lower.Contains("receita")
                    ? ("finance", "summary", "resumo financeiro periódico")
                    : ("stock", "scanAlerts", "verificação periódica de alertas de estoque");
        var policy = await automations.CreateAsync(new CreateAutomationPolicyRequest(
            $"Automação: {label}", specialist, action, "Restaurante atual", "America/Sao_Paulo", 1440, DateTime.UtcNow, null, 31), cancellationToken);
        if (policy is null) return null;
        return new("deterministic", $"Criei o rascunho da política “{policy.Name}”. Ela está DESATIVADA e em dry-run, com execução diária no fuso {policy.TimeZone}. Revise o alvo, horário e limite; só será ativada após confirmação explícita do gestor.", "Política de automação do ISM", "Rascunho criado; nenhuma execução ou alteração foi realizada.")
        {
            Specialist = "assistant",
            ToolName = "assistant.proposeAutomation",
            DecisionCode = AgentDecisionCode.NeedsApproval
        };
    }

    private async Task<AssistantProposal?> BuildProposalAsync(ISM.Domain.Modules.System.Entities.SystemAlert alert, int restaurantId, CancellationToken cancellationToken)
    {
        var entityType = alert.ReferenceEntityType ?? "Alert";
        var entityId = alert.ReferenceEntityId ?? alert.Id;
        decimal original = 0, proposed = 0, cost = 0;
        var version = 0;
        decimal? margin = null;
        var name = alert.Title;
        if (alert.AlertType == SystemAlertType.DishCmvDefasagem && alert.ReferenceEntityId.HasValue && await dishes.GetDishByIdAsync(alert.ReferenceEntityId.Value, cancellationToken) is { } dish)
        {
            name = dish.Name; original = dish.Price; cost = dish.Cost; version = dish.PriceRevision;
            if (!string.IsNullOrWhiteSpace(alert.PayloadSerializedJson))
            {
                using var json = JsonDocument.Parse(alert.PayloadSerializedJson);
                var root = json.RootElement;
                if (root.TryGetProperty("SuggestedPrice", out var suggested)) proposed = suggested.GetDecimal();
                if (root.TryGetProperty("TargetMarginPercent", out var target)) margin = target.GetDecimal();
                if (root.TryGetProperty("CurrentCmvPercent", out var cmv)) cost = original * cmv.GetDecimal() / 100m;
            }
        }
        return new AssistantProposal
        {
            RestaurantId = restaurantId, AlertId = alert.Id,
            Specialist = alert.AlertType == SystemAlertType.DishCmvDefasagem ? "Precificação" : alert.ReferenceEntityType == "Product" ? "Estoque" : "Fornecedores",
            ActionType = alert.AlertType == SystemAlertType.DishCmvDefasagem ? "UpdateDishPrice" : "ReviewAlert",
            EntityType = entityType, EntityId = entityId, EntityName = name, Status = AssistantProposalStatus.Pending,
            OriginalValue = original, ProposedValue = proposed, SnapshotCost = cost, SnapshotTargetMarginPercent = margin,
            EvidenceJson = alert.PayloadSerializedJson ?? "{}", GeneratedAtUtc = alert.GeneratedAtUtc,
            ExpiresAtUtc = alert.GeneratedAtUtc.AddDays(7),
            IdempotencyKey = $"alert:{alert.Id}:action:{(alert.AlertType == SystemAlertType.DishCmvDefasagem ? "price" : "review")}",
            Version = version
        };
    }

    private async Task<string> BuildContextAsync(int restaurantId, CancellationToken cancellationToken)
    {
        var page = await alerts.GetPagedAsync(restaurantId, 1, 12, isDismissed: false, cancellationToken: cancellationToken);
        return string.Join('\n', page.Items.Select(a => $"- {a.Title}: {a.Message}"));
    }

    private async Task<AssistantContextResponse?> BuildAlertContextAsync(ISM.Domain.Modules.System.Entities.SystemAlert alert, int restaurantId, CancellationToken cancellationToken)
    {
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var entityName = alert.Title;
        if (alert.ReferenceEntityType == "Product" && alert.ReferenceEntityId is int productId && await products.GetByIdAsync(productId, cancellationToken) is { } product && product.RestaurantId == restaurantId)
        {
            entityName = product.Name;
            facts["quantidadeAtual"] = Format(product.CurrentQuantity);
            facts["quantidadeMinima"] = Format(product.MinimumQuantity);
            facts["unidade"] = product.Unit;
            facts["custoMedio"] = Format(product.AverageCost);
            facts["consumoMedioDiario"] = Format(product.MovingAverageConsumption);
        }
        if (!string.IsNullOrWhiteSpace(alert.PayloadSerializedJson))
        {
            try
            {
                using var json = JsonDocument.Parse(alert.PayloadSerializedJson);
                foreach (var property in json.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                        facts[ToFactName(property.Name)] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();
                    if (property.Name is "Name" or "ProductName" or "DishName") entityName = property.Value.ToString();
                }
            }
            catch (JsonException) { }
        }
        return new(alert.Id, alert.AlertType.ToString(), alert.Severity.ToString(), alert.Title, alert.Message, alert.ReferenceEntityType, alert.ReferenceEntityId, entityName, facts, alert.GeneratedAtUtc);
    }

    private static string BuildOpeningMessage(AssistantContextResponse context)
    {
        if (context.ReferenceEntityType == "Product" && context.Facts.TryGetValue("quantidadeAtual", out var current) && context.Facts.TryGetValue("quantidadeMinima", out var minimum))
        {
            var unit = context.Facts.TryGetValue("unidade", out var value) ? $" {value}" : string.Empty;
            return $"Vamos analisar o alerta de estoque de {context.EntityName}. No momento, existem {current}{unit} e o mínimo é {minimum}{unit}. O que você gostaria de saber?";
        }
        return $"Vamos analisar o alerta “{context.Title}”. {context.Message} O que você gostaria de saber?";
    }

    private static string? BuildDeterministicResponse(string message, AssistantContextResponse? context)
    {
        if (context is null) return null;
        var lower = message.ToLowerInvariant();
        var current = context.Facts.TryGetValue("quantidadeAtual", out var currentValue) ? currentValue : null;
        var minimum = context.Facts.TryGetValue("quantidadeMinima", out var minimumValue) ? minimumValue : null;
        var unit = context.Facts.TryGetValue("unidade", out var unitValue) ? $" {unitValue}" : string.Empty;
        if ((lower.Contains("por que") || lower.Contains("porque") || lower.Contains("entrou")) && current is not null && minimum is not null)
            return $"{context.EntityName} entrou em alerta porque o saldo atual é {current}{unit}, abaixo do mínimo configurado de {minimum}{unit}. Esse valor vem do estoque autorizado do restaurante em {context.GeneratedAtUtc.ToLocalTime():dd/MM/yyyy HH:mm}.";
        if ((lower.Contains("comprar") || lower.Contains("repor") || lower.Contains("agora")) && current is not null && minimum is not null)
            return $"O ISM confirma o risco de estoque: {context.EntityName} está em {current}{unit} e o mínimo é {minimum}{unit}. Não tenho dados suficientes de prazo de entrega ou pedido aberto para decidir uma compra; confirme a necessidade no módulo de estoque.";
        if (lower.Contains("histórico") || lower.Contains("historico")) return "Não tenho histórico suficiente vinculado a este alerta para concluir uma tendência. O alerta atual e seus números permanecem disponíveis; consulte o histórico de movimentações no módulo correspondente.";
        if (lower.Contains("alerta") || lower.Contains("detalhe")) return $"O alerta é “{context.Title}”, classificado como {context.Severity}. {context.Message}";
        return null;
    }

    private async Task<AssistantChatResponse> BuildFinancialResponseAsync(CancellationToken cancellationToken)
    {
        var overview = await finance.GetAsync(1, 1, cancellationToken);
        var summary = overview.Summary;
        var message = $"Faturamento do mês, baseado em pedidos pagos: R$ {summary.SalesRevenue:N2}. " +
                      $"Outras receitas liquidadas: R$ {summary.OtherIncome:N2}. " +
                      $"Despesas liquidadas: R$ {summary.Expenses:N2}. " +
                      $"Contas a receber: R$ {summary.AccountsReceivable:N2}. " +
                      $"Contas a pagar: R$ {summary.AccountsPayable:N2}.";
        return new("deterministic", message, "Dados financeiros do ISM", null);
    }

    private async Task<AssistantChatResponse?> ExecuteIntentAsync(AgentIntent intent, CancellationToken cancellationToken)
    {
        var result = await tools.ExecuteAsync(new AgentToolExecuteRequest(intent.ToolName, intent.Input), cancellationToken);
        if (result.Code == AgentDecisionCode.Unreachable)
            return new("deterministic_fallback", result.Message, "Dados determinísticos do ISM", "Nenhuma ação foi executada.") { Specialist = intent.Specialist, ToolName = intent.ToolName, DecisionCode = result.Code, Evidence = result.Evidence };

        var message = DescribeToolResult(result);
        var warning = result.Code == AgentDecisionCode.CanAct ? null : "A resposta foi limitada pela disponibilidade ou qualidade dos dados; nenhuma ação foi executada.";
        var source = intent.ToolName == "finance.getRevenueSummary" ? "Dados financeiros do ISM" : $"Dados determinísticos do ISM · {intent.Specialist}";
        return new("deterministic", message, source, warning)
        {
            Specialist = intent.Specialist,
            ToolName = intent.ToolName,
            DecisionCode = result.Code,
            Evidence = result.Evidence
        };
    }

    private static string DescribeToolResult(AgentToolResult result)
    {
        if (result.Data is null)
            return result.Code == AgentDecisionCode.InsufficientData ? $"Dados insuficientes: {result.Message}" : result.Message;
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result.Data));
        var root = document.RootElement;
        string Text(string name) => root.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;
        string Quantity(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            ? number.ToString("0.###", CultureInfo.GetCultureInfo("pt-BR")) : Text(name);
        var description = result.ToolName switch
        {
            "stock.analyzeReplenishment" => $"{Text("ProductName")}: saldo {Quantity("CurrentQuantity")}, consumo médio {Quantity("DailyConsumption")}/dia e aproximadamente {Quantity("DaysRemaining")} dias restantes. {result.Message}",
            "pricing.simulateDishPrice" => $"{Text("DishName")}: CMV atual {Text("CurrentCmvPercent")}%, custo verificado R$ {Text("VerifiedCost")} e preço simulado R$ {Text("SuggestedPrice")}. {result.Message}",
            "suppliers.compareOffers" => $"Encontrei {root.GetProperty("Offers").GetArrayLength()} ofertas recentes e compatíveis para {Text("ProductName")} ({Text("Unit")}). {result.Message}",
            "finance.getPeriodSummary" => $"Período consultado: receita de pedidos pagos R$ {Text("PaidSalesRevenue")}, outras entradas R$ {Text("SettledOtherIncome")}, despesas R$ {Text("SettledExpenses")} e fluxo líquido R$ {Text("NetCashFlow")}. {result.Message}",
            "finance.getRevenueSummary" => $"Faturamento do mês, baseado em pedidos pagos: R$ {Text("SalesRevenue")}. Outras receitas liquidadas: R$ {Text("OtherIncome")}. Despesas liquidadas: R$ {Text("Expenses")}. Contas a receber: R$ {Text("AccountsReceivable")}. Contas a pagar: R$ {Text("AccountsPayable")}.",
            "stock.getProduct" => $"{Text("Name")}: saldo atual {Quantity("CurrentQuantity")} e mínimo {Quantity("MinimumQuantity")} {Text("Unit")}. {result.Message}",
            "pricing.getDishCost" => $"{Text("Name")}: preço R$ {Text("Price")}, custo R$ {Text("Cost")}. {result.Message}",
            "assistant.getAlertContext" => $"Alerta: {Text("Title")}. {Text("Message")}. {result.Message}",
            _ => result.Message
        };
        var evidence = result.Evidence.Count == 0
            ? string.Empty
            : $" Fonte: {string.Join(", ", result.Evidence.Select(item => item.SourceType))}, observado em {result.Evidence.Max(item => item.ObservedAtUtc).ToLocalTime():dd/MM/yyyy HH:mm}.";
        return description + evidence;
    }

    private static bool IsFinancialQuestion(string message)
    {
        var lower = message.ToLowerInvariant();
        return lower.Contains("faturamento") || lower.Contains("receita") || lower.Contains("despesa") ||
               lower.Contains("conta a pagar") || lower.Contains("contas a pagar") || lower.Contains("fluxo de caixa");
    }

    private async Task<AssistantConversationResponse> BuildConversationAsync(AssistantThread thread, AssistantContextResponse? context, CancellationToken cancellationToken)
    {
        var messages = await conversations.GetMessagesAsync(thread.Id, thread.RestaurantId, thread.UserId, 200, cancellationToken);
        return new(MapThread(thread, context), messages.Select(MapMessage).ToList());
    }

    private AssistantThreadResponse MapThread(AssistantThread thread) => MapThread(thread, DeserializeContext(thread.ContextSnapshotJson));
    private static AssistantThreadResponse MapThread(AssistantThread thread, AssistantContextResponse? context) => new(thread.Id, thread.ParentThreadId, thread.OriginAlertId, thread.OriginType, thread.Subject, context, thread.CreatedAtUtc, thread.LastMessageAtUtc);
    private static AssistantMessageResponse MapMessage(AssistantMessage message) => new(message.Id, message.Role, message.Content, message.Mode, message.Source, message.CreatedAtUtc);
    private static AssistantContextResponse? DeserializeContext(string json) => string.IsNullOrWhiteSpace(json) || json == "{}" ? null : JsonSerializer.Deserialize<AssistantContextResponse>(json);
    private static string ToFactName(string name) => name switch
    {
        "CurrentQuantity" => "quantidadeAtual", "MinimumQuantity" => "quantidadeMinima", "MovingAverageConsumption" => "consumoMedioDiario",
        "DaysRemaining" => "diasRestantes", "SuggestedPrice" => "precoSugerido", "CurrentCmvPercent" => "cmvAtualPercentual",
        "TargetMarginPercent" => "margemAlvoPercentual", _ => string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..]
    };
    private static string Format(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string? Limit(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= max ? value : value[..max];
    private static AssistantActionResponse Map(AssistantProposal p) => new(p.Id, p.AlertId, p.Specialist, p.ActionType, p.EntityType, p.EntityId, p.EntityName, p.Status, p.OriginalValue, p.ProposedValue, p.SnapshotCost, p.SnapshotTargetMarginPercent, p.EvidenceJson, p.GeneratedAtUtc, p.DecidedByUserId, p.DecidedAtUtc, p.ExpiresAtUtc, p.Version);
}
