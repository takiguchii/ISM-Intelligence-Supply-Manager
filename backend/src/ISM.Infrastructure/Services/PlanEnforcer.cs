using ISM.Application.Services;
using ISM.Domain.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Services;

public sealed class PlanEnforcer : IPlanEnforcer
{
    private readonly IsmDbContext _db;

    public PlanEnforcer(IsmDbContext db)
    {
        _db = db;
    }

    public async Task AssertCanAddUserAsync(int restaurantId, CancellationToken ct)
    {
        var (plano, _) = await GetPlanoAsync(restaurantId, ct);
        var count = await _db.Users.CountAsync(u => u.RestaurantId == restaurantId, ct);
        if (count >= plano.MaxUsuarios)
            throw new InvalidOperationException(
                $"Limite de usuários do plano {plano.Nome} atingido ({plano.MaxUsuarios}). Faça upgrade para adicionar mais.");
    }

    public async Task AssertCanAddCategoryAsync(int restaurantId, CancellationToken ct)
    {
        var (plano, _) = await GetPlanoAsync(restaurantId, ct);
        var count = await _db.Categories.CountAsync(c => c.RestaurantId == restaurantId, ct);
        if (count >= plano.MaxCategorias)
            throw new InvalidOperationException(
                $"Limite de categorias do plano {plano.Nome} atingido ({plano.MaxCategorias}).");
    }

    public async Task AssertCanAddProductAsync(int restaurantId, CancellationToken ct)
    {
        var (plano, _) = await GetPlanoAsync(restaurantId, ct);
        var count = await _db.Products.CountAsync(p => p.RestaurantId == restaurantId, ct);
        if (count >= plano.MaxProdutos)
            throw new InvalidOperationException(
                $"Limite de produtos do plano {plano.Nome} atingido ({plano.MaxProdutos}).");
    }

    public async Task AssertCanAddDishAsync(int restaurantId, CancellationToken ct)
    {
        var (plano, _) = await GetPlanoAsync(restaurantId, ct);
        var count = await _db.Dishes.CountAsync(d => d.RestaurantId == restaurantId, ct);
        if (count >= plano.MaxPratos)
            throw new InvalidOperationException(
                $"Limite de pratos do plano {plano.Nome} atingido ({plano.MaxPratos}).");
    }

    public async Task<(bool Ativo, string? Mensagem)> ValidateRestaurantAccessAsync(int? restaurantId, CancellationToken ct)
    {
        if (!restaurantId.HasValue) return (true, null);

        var restaurant = await _db.Restaurants
            .Include(r => r.Plano)
            .FirstOrDefaultAsync(r => r.Id == restaurantId.Value, ct);

        if (restaurant == null) return (false, "Restaurante não encontrado.");
        if (!restaurant.PlanoAtivo) return (false, "Assinatura do restaurante está suspensa.");
        if (restaurant.TrialEndAtUtc.HasValue && restaurant.TrialEndAtUtc.Value < DateTime.UtcNow && restaurant.Plano?.PrecoMensal == 0)
            return (false, "Período trial expirou. Ative sua assinatura para continuar.");

        return (true, null);
    }

    private async Task<(Plano Plano, Restaurant Restaurant)> GetPlanoAsync(int restaurantId, CancellationToken ct)
    {
        var restaurant = await _db.Restaurants
            .Include(r => r.Plano)
            .FirstOrDefaultAsync(r => r.Id == restaurantId, ct);

        if (restaurant == null)
            throw new InvalidOperationException($"Restaurante com ID {restaurantId} não existe.");

        if (!restaurant.PlanoAtivo)
            throw new InvalidOperationException("Assinatura do restaurante está suspensa.");

        var plano = restaurant.Plano ?? new Plano
        {
            Nome = "Free",
            MaxUsuarios = 2,
            MaxPratos = 10,
            MaxProdutos = 20,
            MaxCategorias = 3,
            PrecoMensal = 0,
            Ativo = true
        };

        return (plano, restaurant);
    }
}
