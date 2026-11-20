using System.Text.Json;
using ISM.Application.Interfaces.Assistant;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Assistant.Entities;
using ISM.Domain.Modules.Assistant.Enums;

namespace ISM.Application.Services.Assistant;

public sealed class AutomationRunner(
    IAutomationRepository repository,
    ISystemAlertRepository alerts,
    TimeProvider clock) : IAutomationRunner
{
    public async Task<int> RunDueAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await repository.ExpireAsync(restaurantId, now, cancellationToken);
        var due = await repository.GetDueAsync(restaurantId, now, cancellationToken);
        var executed = 0;
        foreach (var policy in due)
        {
            var scheduledAt = policy.NextRunAtUtc;
            if (!await repository.TryClaimAsync(policy, now, cancellationToken)) continue;

            var key = $"policy:{policy.Id}:scheduled:{scheduledAt.Ticks}";
            var started = now;
            var evidence = await BuildEvidenceAsync(policy, restaurantId, cancellationToken);
            await repository.AddRunAsync(new AutomationRun
            {
                RestaurantId = restaurantId, PolicyId = policy.Id, Status = AutomationRunStatus.Simulated,
                IdempotencyKey = key, ScheduledAtUtc = scheduledAt, StartedAtUtc = started,
                FinishedAtUtc = clock.GetUtcNow().UtcDateTime,
                Reason = policy.Mode == AutomationMode.DryRun
                    ? "Execução simulada em dry-run; nenhuma mutação ou chamada externa foi realizada."
                    : "Política ativa limitada a análise; nenhuma mutação ou chamada externa foi realizada.",
                EvidenceJson = JsonSerializer.Serialize(evidence), CorrelationId = Guid.NewGuid().ToString("N")
            }, cancellationToken);
            executed++;
        }
        return executed;
    }

    private async Task<object> BuildEvidenceAsync(AutomationPolicy policy, int restaurantId, CancellationToken cancellationToken)
    {
        if (policy.ActionType.Equals("scanalerts", StringComparison.OrdinalIgnoreCase))
        {
            var page = await alerts.GetPagedAsync(restaurantId, 1, 1, isDismissed: false, cancellationToken: cancellationToken);
            return new { source = "SystemAlert", observedAtUtc = clock.GetUtcNow().UtcDateTime, unreadOrActiveAlerts = page.TotalCount, target = policy.Target };
        }

        return new
        {
            source = "PolicySchedule",
            observedAtUtc = clock.GetUtcNow().UtcDateTime,
            specialist = policy.Specialist,
            action = policy.ActionType,
            target = policy.Target,
            note = "Ações mutáveis permanecem bloqueadas nesta etapa."
        };
    }
}
