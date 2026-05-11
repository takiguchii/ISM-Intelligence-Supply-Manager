using FluentAssertions;
using ISM.Application.Services;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.UnitTests;

public sealed class SystemStatusServiceTests
{
    [Fact]
    public async Task GetCurrentStatusAsync_ShouldReturnDisconnectedPayload_WhenDatabaseIsUnavailable()
    {
        var repository = new FakePlatformModuleRepository(canConnect: false);
        var service = new SystemStatusService(repository);

        var response = await service.GetCurrentStatusAsync();

        response.DatabaseConnected.Should().BeFalse();
        response.EnabledModuleCount.Should().Be(0);
        response.EnabledModules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentStatusAsync_ShouldMapEnabledModules_WhenDatabaseIsAvailable()
    {
        var repository = new FakePlatformModuleRepository(
            canConnect: true,
            modules:
            [
                new PlatformModule
                {
                    Id = 1,
                    Name = "Foundation",
                    Slug = "foundation",
                    Description = "Core module",
                    IsEnabled = true,
                    SortOrder = 1
                },
                new PlatformModule
                {
                    Id = 2,
                    Name = "Analytics",
                    Slug = "analytics",
                    Description = "Future insights",
                    IsEnabled = true,
                    SortOrder = 2
                }
            ]);

        var service = new SystemStatusService(repository);

        var response = await service.GetCurrentStatusAsync();

        response.DatabaseConnected.Should().BeTrue();
        response.EnabledModuleCount.Should().Be(2);
        response.FirstEnabledModule?.Slug.Should().Be("foundation");
        response.EnabledModules.Select(module => module.Slug).Should().ContainInOrder("foundation", "analytics");
    }

    private sealed class FakePlatformModuleRepository : IPlatformModuleRepository
    {
        private readonly bool _canConnect;
        private readonly IReadOnlyList<PlatformModule> _modules;

        public FakePlatformModuleRepository(bool canConnect, IReadOnlyList<PlatformModule>? modules = null)
        {
            _canConnect = canConnect;
            _modules = modules ?? Array.Empty<PlatformModule>();
        }

        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_canConnect);

        public Task<int> CountEnabledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_modules.Count(module => module.IsEnabled));

        public Task<IReadOnlyList<PlatformModule>> GetEnabledOrderedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PlatformModule>>(_modules.OrderBy(module => module.SortOrder).ToArray());

        public Task<PlatformModule?> GetFirstEnabledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_modules.OrderBy(module => module.SortOrder).FirstOrDefault());
    }
}
