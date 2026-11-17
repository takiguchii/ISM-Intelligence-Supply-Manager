using FluentAssertions;
using ISM.Application.DTOs.Assistant;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Security;
using ISM.Application.Services.Assistant;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Assistant.Entities;
using ISM.Domain.Modules.Assistant.Enums;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;

namespace ISM.UnitTests;

public sealed class AutomationSafetyTests
{
    [Fact]
    public async Task Policy_UsesControlledClock_AndExpiresWithoutRunning()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero));
        var repository = new AutomationRepositoryFake();
        var service = new AutomationService(repository, new TestCurrentUser(), clock);
        var policy = await service.CreateAsync(new CreateAutomationPolicyRequest("Estoque", "stock", "scanAlerts", "restaurante", "America/Sao_Paulo", 60, null, clock.GetUtcNow().UtcDateTime.AddHours(1)));

        policy.Should().NotBeNull();
        clock.Advance(TimeSpan.FromHours(2));
        (await service.ListAsync()).Single().Status.Should().Be(AutomationPolicyStatus.Draft);

        var confirmed = await service.ConfirmAsync(policy!.Id, new ConfirmAutomationRequest(true));
        confirmed.StatusCode.Should().Be(409);
        confirmed.Policy!.Status.Should().Be(AutomationPolicyStatus.Expired);
    }

    [Fact]
    public async Task Runner_UsesScheduledKey_AndDoesNotRunSameSlotTwice()
    {
        var now = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
        var clock = new ManualClock(now);
        var policy = new AutomationPolicy
        {
            Id = 7, RestaurantId = 42, OwnerUserId = 1, Name = "Estoque", Specialist = "stock",
            ActionType = "scanAlerts", Target = "restaurante", IntervalMinutes = 60,
            Status = AutomationPolicyStatus.Active, Mode = AutomationMode.DryRun,
            StartsAtUtc = now.UtcDateTime.AddMinutes(-1), NextRunAtUtc = now.UtcDateTime.AddMinutes(-1), MaxRunsPerPeriod = 5
        };
        var repository = new AutomationRepositoryFake(policy);
        var runner = new AutomationRunner(repository, new AlertRepositoryFake(), clock);

        (await runner.RunDueAsync(42)).Should().Be(1);
        (await runner.RunDueAsync(42)).Should().Be(0);
        repository.Runs.Should().ContainSingle().Which.Status.Should().Be(AutomationRunStatus.Simulated);
    }

    private sealed class ManualClock(DateTimeOffset current) : TimeProvider
    {
        private DateTimeOffset _current = current;
        public override DateTimeOffset GetUtcNow() => _current;
        public void Advance(TimeSpan amount) => _current = _current.Add(amount);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public int UserId => 1;
        public string? Email => "manager@test.local";
        public string Role => IsmRoles.Manager;
        public int? RestaurantId => 42;
        public bool IsSuperAdmin => false;
        public bool IsManagerOrAbove => true;
    }

    private sealed class AutomationRepositoryFake(AutomationPolicy? initial = null) : IAutomationRepository
    {
        private readonly List<AutomationPolicy> _policies = initial is null ? [] : [initial];
        public List<AutomationRun> Runs { get; } = [];

        public Task<IReadOnlyList<AutomationPolicy>> ListAsync(int restaurantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AutomationPolicy>>(_policies.Where(p => p.RestaurantId == restaurantId).ToList());
        public Task<AutomationPolicy?> GetAsync(int id, int restaurantId, CancellationToken cancellationToken = default)
            => Task.FromResult(_policies.SingleOrDefault(p => p.Id == id && p.RestaurantId == restaurantId));
        public Task<AutomationPolicy> AddAsync(AutomationPolicy policy, CancellationToken cancellationToken = default)
        {
            policy.Id = _policies.Count + 1;
            _policies.Add(policy);
            return Task.FromResult(policy);
        }
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AutomationRun>> ListRunsAsync(int policyId, int restaurantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AutomationRun>>(Runs.Where(r => r.PolicyId == policyId && r.RestaurantId == restaurantId).ToList());
        public Task<IReadOnlyList<AutomationPolicy>> GetDueAsync(int restaurantId, DateTime nowUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AutomationPolicy>>(_policies.Where(p => p.RestaurantId == restaurantId && p.Status == AutomationPolicyStatus.Active && p.NextRunAtUtc <= nowUtc && (p.ExpiresAtUtc is null || p.ExpiresAtUtc > nowUtc) && p.RunsThisPeriod < p.MaxRunsPerPeriod).ToList());
        public Task<int> ExpireAsync(int restaurantId, DateTime nowUtc, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> TryClaimAsync(AutomationPolicy policy, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            policy.LastRunAtUtc = nowUtc;
            policy.NextRunAtUtc = nowUtc.AddMinutes(policy.IntervalMinutes);
            policy.RunsThisPeriod++;
            return Task.FromResult(true);
        }
        public Task AddRunAsync(AutomationRun run, CancellationToken cancellationToken = default)
        {
            run.Id = Runs.Count + 1;
            Runs.Add(run);
            return Task.CompletedTask;
        }
    }

    private sealed class AlertRepositoryFake : ISystemAlertRepository
    {
        public Task<SystemAlert?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<SystemAlert?>(null);
        public Task<(IReadOnlyList<SystemAlert> Items, int TotalCount)> GetPagedAsync(int? restaurantId, int pageNumber, int pageSize, SystemAlertType? alertType = null, AlertSeverity? severity = null, bool? isRead = null, bool? isDismissed = null, string? search = null, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<SystemAlert>, int)>(([], 0));
        public Task<SystemAlert> CreateAsync(SystemAlert alert, CancellationToken cancellationToken = default) => Task.FromResult(alert);
        public Task<SystemAlert?> UpdateAsync(SystemAlert alert, CancellationToken cancellationToken = default) => Task.FromResult<SystemAlert?>(alert);
        public Task<bool> ExistsSameTypeNonDismissedWithinAsync(int restaurantId, SystemAlertType alertType, string? referenceEntityType, int? referenceEntityId, TimeSpan withinWindow, string? deduplicationKey = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
