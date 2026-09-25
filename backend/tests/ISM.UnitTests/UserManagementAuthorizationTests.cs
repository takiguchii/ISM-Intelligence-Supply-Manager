using FluentAssertions;
using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Application.Services.Users;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.UnitTests;

public sealed class UserManagementAuthorizationTests
{
    [Theory]
    [InlineData(IsmRoles.Waiter)]
    [InlineData(IsmRoles.Chef)]
    [InlineData(IsmRoles.Manager)]
    public async Task UpdateUserAsync_ShouldRejectNonRestaurantAdmin(string role)
    {
        var service = CreateService(role, 10);

        var action = () => service.UpdateUserAsync(2, Request(10, IsmRoles.Admin));

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldAllowRestaurantAdminToManageOwnTenantUser()
    {
        var userRepository = new InMemoryUserRepository(new User
        {
            Id = 2, Name = "Colaborador", Email = "colaborador@ism.test", Role = IsmRoles.Waiter,
            RestaurantId = 10, IsActive = true, CreatedAtUtc = DateTime.UtcNow
        });
        var service = new UserService(userRepository, new InMemoryRestaurantRepository(10),
            new TestCurrentUser(IsmRoles.Admin, 10));

        var result = await service.UpdateUserAsync(2, Request(10, IsmRoles.Manager));

        result.Role.Should().Be(IsmRoles.Manager);
        userRepository.Saved.Should().BeTrue();
    }

    private static UserService CreateService(string role, int restaurantId) =>
        new(new InMemoryUserRepository(new User
            { Id = 2, Name = "Colaborador", Email = "colaborador@ism.test", Role = IsmRoles.Waiter, RestaurantId = restaurantId }),
            new InMemoryRestaurantRepository(restaurantId), new TestCurrentUser(role, restaurantId));

    private static UpdateUserRequest Request(int restaurantId, string role) => new()
    {
        Name = "Colaborador", Email = "colaborador@ism.test", Role = role, RestaurantId = restaurantId, IsActive = true
    };

    private sealed class TestCurrentUser(string role, int? restaurantId) : ICurrentUser
    {
        public int UserId => 1;
        public string? Email => "admin@ism.test";
        public string Role => role;
        public int? RestaurantId => restaurantId;
        public bool IsSuperAdmin => Role == IsmRoles.Admin && !RestaurantId.HasValue;
        public bool IsManagerOrAbove => Role is IsmRoles.Admin or IsmRoles.Manager;
    }

    private sealed class InMemoryUserRepository(params User[] users) : IUserRepository
    {
        private readonly List<User> _users = users.ToList();
        public bool Saved { get; private set; }
        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(_users.SingleOrDefault(x => x.Id == id));
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(_users.SingleOrDefault(x => x.Email == email));
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(_users);
        public Task<IReadOnlyList<User>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(_users.Where(x => x.RestaurantId == restaurantId).ToList());
        public Task AddAsync(User user, CancellationToken cancellationToken = default) { _users.Add(user); return Task.CompletedTask; }
        public void Update(User user) { }
        public void Delete(User user) => _users.Remove(user);
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(_users.Any(x => x.Email == email));
        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) { Saved = true; return Task.FromResult(true); }
    }

    private sealed class InMemoryRestaurantRepository(int restaurantId) : IRestaurantRepository
    {
        private readonly Restaurant _restaurant = new() { Id = restaurantId, Name = "Restaurante", CNPJ = "00000000000000", Created = DateTime.UtcNow };
        public Task<Restaurant?> GetRestaurantByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Restaurant?>(id == _restaurant.Id ? _restaurant : null);
        public Task<IReadOnlyList<Restaurant>> GetAllRestaurantsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Restaurant>>([_restaurant]);
        public Task AddRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Restaurant restaurant) { }
        public void Delete(Restaurant restaurant) { }
        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
