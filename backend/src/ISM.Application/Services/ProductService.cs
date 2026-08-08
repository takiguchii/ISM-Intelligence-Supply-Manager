using ISM.Application.Interfaces;
using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Interfaces;

namespace ISM.Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPlanEnforcer _planEnforcer;

    public ProductService(
        IProductRepository productRepository,
        ICurrentUser currentUser,
        IPlanEnforcer planEnforcer)
    {
        _productRepository = productRepository;
        _currentUser = currentUser;
        _planEnforcer = planEnforcer;
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (product.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
        }

        return Map(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            products = products.Where(p => p.RestaurantId == _currentUser.RestaurantId.Value).ToList();
        }
        return products.Select(Map).ToArray();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var restaurantId = ResolveRestaurantId(request.RestaurantId);

        if (!_currentUser.IsSuperAdmin && !_currentUser.IsManagerOrAbove)
            throw new UnauthorizedAccessException("Apenas gerentes podem criar produtos.");

        await _planEnforcer.AssertCanAddProductAsync(restaurantId, cancellationToken);

        var product = new Product
        {
            RestaurantId = restaurantId,
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            CurrentQuantity = request.CurrentQuantity,
            MinimumQuantity = request.MinimumQuantity,
            AverageCost = request.AverageCost,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product, cancellationToken);
        return Map(product);
    }

    public async Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem editar produtos.");
        }

        var updated = new Product
        {
            Id = id,
            RestaurantId = existing.RestaurantId,
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            CurrentQuantity = request.CurrentQuantity,
            MinimumQuantity = request.MinimumQuantity,
            AverageCost = request.AverageCost,
            IsActive = existing.IsActive,
            CreatedAtUtc = existing.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _productRepository.UpdateAsync(updated, cancellationToken);
        return Map(updated);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null) return false;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return false;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem deletar produtos.");
        }

        return await _productRepository.DeleteAsync(id, cancellationToken);
    }

    private int ResolveRestaurantId(int requestRestaurantId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            if (requestRestaurantId <= 0)
                throw new InvalidOperationException("Super Admin deve informar o RestaurantId.");
            return requestRestaurantId;
        }

        if (!_currentUser.RestaurantId.HasValue)
            throw new UnauthorizedAccessException("Usuário não vinculado a restaurante.");

        if (requestRestaurantId > 0 && requestRestaurantId != _currentUser.RestaurantId.Value)
            throw new UnauthorizedAccessException("Você só pode criar produtos no seu restaurante.");

        return _currentUser.RestaurantId.Value;
    }

    private static ProductResponse Map(Product product)
        => new(
            product.Id,
            product.RestaurantId,
            product.Name,
            product.Unit,
            product.CurrentQuantity,
            product.MinimumQuantity,
            product.AverageCost,
            product.IsActive,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
}
