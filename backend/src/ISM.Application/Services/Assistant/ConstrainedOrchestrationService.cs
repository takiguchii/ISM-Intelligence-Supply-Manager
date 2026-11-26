using System.Collections.Concurrent;
using System.Text.Json;
using ISM.Application.Agents;

namespace ISM.Application.Services.Assistant;

public sealed class ConstrainedOrchestrationService(
    IAgentToolRegistry registry,
    IAgentIntentRouter intentRouter) : IOrchestrationPlanner, IOrchestrationExecutor, IOrchestrationSynthesizer
{
    private const int MaxSteps = 5;
    private const int MaxConcurrency = 2;
    private const int BudgetMilliseconds = 90000;

    public Task<ValidatedOrchestrationPlan> PlanAsync(AssistantQueryContext query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var correlationId = Guid.NewGuid().ToString("N");
        var steps = new List<OrchestrationPlanStep>();
        var question = query.Question.Trim();

        if (question.Contains("restaurantid", StringComparison.OrdinalIgnoreCase)
            || question.Contains("outro restaurante", StringComparison.OrdinalIgnoreCase)
            || question.Contains("tenant", StringComparison.OrdinalIgnoreCase)
            || question.Contains("invente", StringComparison.OrdinalIgnoreCase)
            || question.Contains("ignore", StringComparison.OrdinalIgnoreCase)
            || question.Contains("pague", StringComparison.OrdinalIgnoreCase)
            || question.Contains("compre", StringComparison.OrdinalIgnoreCase)
            || question.Contains("pix", StringComparison.OrdinalIgnoreCase)
            || question.Contains("delete", StringComparison.OrdinalIgnoreCase)
            || question.Contains("sql", StringComparison.OrdinalIgnoreCase)
            || question.Contains("segredo", StringComparison.OrdinalIgnoreCase)
            || question.Contains("aprove", StringComparison.OrdinalIgnoreCase)
            || question.Contains("altere", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new ValidatedOrchestrationPlan(correlationId, query, [], MaxSteps, 2, BudgetMilliseconds));

        AddDomainIntents(steps, question);
        if (steps.Count == 0) AddIntent(steps, intentRouter.Resolve(question));
        AddSubjectIntent(steps, query);
        AddSupplierIntent(steps, question);

        var distinct = steps
            .GroupBy(step => step.ToolName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(MaxSteps)
            .ToArray();
        return Task.FromResult(new ValidatedOrchestrationPlan(correlationId, query, distinct, MaxSteps, 2, BudgetMilliseconds));
    }

    public async Task<OrchestrationResult> ExecuteAsync(ValidatedOrchestrationPlan plan, CancellationToken cancellationToken = default)
    {
        if (plan.Steps.Count > MaxSteps || plan.Steps.Any(step => step.DependsOn.Count > 0))
            return new(plan.CorrelationId, [], "O plano solicitado excede os limites de segurança do orquestrador.", true, ["Plano rejeitado por limite de passos ou dependências."], []);

        var allowed = registry.List().Where(tool => tool.Operation == AgentToolOperation.Read).Select(tool => tool.ToolName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (plan.Steps.Any(step => !allowed.Contains(step.ToolName) || step.Input.ValueKind != JsonValueKind.Object || step.Input.TryGetProperty("restaurantId", out _)))
            return new(plan.CorrelationId, [], "O plano contém ferramenta ou argumento não permitido.", true, ["Somente ferramentas de leitura allowlistadas e escopadas pelo servidor podem ser usadas."], []);

        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(BudgetMilliseconds);
        using var gate = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        var results = new ConcurrentBag<OrchestrationStepResult>();
        var tasks = plan.Steps.Select(async step =>
        {
            await gate.WaitAsync(budget.Token);
            try
            {
                var result = await registry.ExecuteAsync(new AgentToolExecuteRequest(step.ToolName, step.Input, plan.CorrelationId) { Specialist = step.Specialist }, budget.Token);
                results.Add(new(step, result));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                results.Add(new(step, new AgentToolResult(step.ToolName, AgentDecisionCode.Unreachable, null, [], "A etapa excedeu o orçamento do orquestrador.", plan.CorrelationId, 0)));
            }
            finally { gate.Release(); }
        });
        try { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
        return Summarize(plan, results.OrderBy(item => item.Step.StepId).ToArray());
    }

    public OrchestrationResult Summarize(ValidatedOrchestrationPlan plan, IReadOnlyList<OrchestrationStepResult> steps)
    {
        var successful = steps.Where(item => item.Result.Code == AgentDecisionCode.CanAct).ToArray();
        var warnings = steps.Where(item => item.Result.Code != AgentDecisionCode.CanAct)
            .Select(item => $"{item.Step.Specialist}: {item.Result.Message}").ToList();
        if (steps.Count == 0)
            warnings.Add("Não foi possível identificar uma consulta autorizada com os dados fornecidos.");
        var lines = successful.Select(item => $"• {AssistantService.DescribeToolResult(item.Result)}").ToList();
        if (lines.Count == 0) lines.Add("Não há dados determinísticos suficientes para concluir a análise.");
        var answer = string.Join("\n", lines);
        return new(plan.CorrelationId, steps, answer, warnings.Count > 0, warnings, steps.SelectMany(item => item.Result.Evidence).ToArray());
    }

    private static void AddIntent(List<OrchestrationPlanStep> steps, AgentIntent? intent)
    {
        if (intent is null || intent.Specialist == "assistant") return;
        steps.Add(new($"{intent.Specialist}-1", intent.Specialist, intent.ToolName, intent.Input, []));
    }

    private static void AddDomainIntents(List<OrchestrationPlanStep> steps, string question)
    {
        if (question.Contains("faturamento", StringComparison.OrdinalIgnoreCase)
            || question.Contains("receita", StringComparison.OrdinalIgnoreCase)
            || question.Contains("despesa", StringComparison.OrdinalIgnoreCase)
            || question.Contains("financeir", StringComparison.OrdinalIgnoreCase)
            || question.Contains("fluxo de caixa", StringComparison.OrdinalIgnoreCase)
            || question.Contains("vendas", StringComparison.OrdinalIgnoreCase)
            || question.Contains("vendemos", StringComparison.OrdinalIgnoreCase)
            || question.Contains("pedidos pagos", StringComparison.OrdinalIgnoreCase))
            steps.Add(new("finance-1", "finance", "finance.getRevenueSummary", JsonSerializer.SerializeToElement(new { }), []));

        if (TryReadId(question, "produto", out var productId)
            && (question.Contains("estoque", StringComparison.OrdinalIgnoreCase) || question.Contains("saldo", StringComparison.OrdinalIgnoreCase) || question.Contains("produto", StringComparison.OrdinalIgnoreCase)))
            steps.Add(new("stock-1", "stock", "stock.getProduct", JsonSerializer.SerializeToElement(new { productId }), []));

        if (TryReadId(question, "prato", out var dishId)
            && (question.Contains("custo", StringComparison.OrdinalIgnoreCase) || question.Contains("cmv", StringComparison.OrdinalIgnoreCase) || question.Contains("preço", StringComparison.OrdinalIgnoreCase) || question.Contains("preco", StringComparison.OrdinalIgnoreCase)))
            steps.Add(new("pricing-1", "pricing", "pricing.getDishCost", JsonSerializer.SerializeToElement(new { dishId }), []));
    }

    private static bool TryReadId(string message, string label, out int id)
    {
        id = 0;
        var marker = message.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return false;
        var digits = new string(message[(marker + label.Length)..].TrimStart().TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out id) && id > 0;
    }

    private static void AddSubjectIntent(List<OrchestrationPlanStep> steps, AssistantQueryContext query)
    {
        if (query.SubjectEntityId is not > 0) return;
        var tool = query.SubjectEntityType?.ToLowerInvariant() switch
        {
            "product" => "stock.getProduct",
            "dish" => "pricing.getDishCost",
            _ => null
        };
        var specialist = tool?.StartsWith("stock", StringComparison.OrdinalIgnoreCase) == true ? "stock" : "pricing";
        if (tool is not null)
        {
            object input = tool.StartsWith("stock", StringComparison.OrdinalIgnoreCase)
                ? new { productId = query.SubjectEntityId.Value }
                : new { dishId = query.SubjectEntityId.Value };
            steps.Add(new($"{specialist}-context", specialist, tool, JsonSerializer.SerializeToElement(input), []));
        }
    }

    private void AddSupplierIntent(List<OrchestrationPlanStep> steps, string question)
    {
        if (!question.Contains("fornecedor", StringComparison.OrdinalIgnoreCase) && !question.Contains("cotação", StringComparison.OrdinalIgnoreCase) && !question.Contains("cotacao", StringComparison.OrdinalIgnoreCase)) return;
        // Só planeja a comparação quando a pergunta identifica produto e unidade.
        // O contexto do chat contém o ID da entidade, mas não o nome necessário
        // para a ferramenta; inventar um nome produziria uma consulta sem valor.
        var intent = intentRouter.Resolve(question);
        if (intent?.ToolName != "suppliers.compareOffers") return;
        steps.Add(new("suppliers-context", intent.Specialist, intent.ToolName, intent.Input, []));
    }
}
