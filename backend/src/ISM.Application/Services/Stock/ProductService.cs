using ISM.Application.Interfaces;
using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Enums;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.Stock;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPlanEnforcer _planEnforcer;
    private readonly IStockMovementRepository _stockMovementRepository;

    public ProductService(
        IProductRepository productRepository,
        ICurrentUser currentUser,
        IPlanEnforcer planEnforcer,
        IStockMovementRepository stockMovementRepository)
    {
        _productRepository = productRepository;
        _currentUser = currentUser;
        _planEnforcer = planEnforcer;
        _stockMovementRepository = stockMovementRepository;
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

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? search = null,
        bool? isCritical = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        int? filterRestaurantId = null;
        if (!_currentUser.IsSuperAdmin)
        {
            filterRestaurantId = _currentUser.RestaurantId ?? 1;
        }

        var (items, totalCount) = await _productRepository.GetPagedAsync(
            filterRestaurantId, pageNumber, pageSize, search, isCritical, cancellationToken);

        var responses = items.Select(Map).ToList();
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<ProductResponse>(
            responses,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1,
            pageNumber < totalPages);
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
            MaximumQuantity = request.MaximumQuantity,
            ReorderPoint = request.ReorderPoint,
            AverageCost = request.AverageCost,
            MovingAverageConsumption = request.MovingAverageConsumption,
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

        var oldQuantity = existing.CurrentQuantity;

        var updated = new Product
        {
            Id = id,
            RestaurantId = existing.RestaurantId,
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            CurrentQuantity = request.CurrentQuantity,
            MinimumQuantity = request.MinimumQuantity,
            MaximumQuantity = request.MaximumQuantity,
            ReorderPoint = request.ReorderPoint,
            AverageCost = request.AverageCost,
            MovingAverageConsumption = request.MovingAverageConsumption,
            LastConsumptionRecalculatedAtUtc = existing.LastConsumptionRecalculatedAtUtc,
            IsActive = existing.IsActive,
            CreatedAtUtc = existing.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _productRepository.UpdateAsync(updated, cancellationToken);

        try
        {
            var delta = request.CurrentQuantity - oldQuantity;
            if (delta != 0m)
            {
                var movement = new StockMovement
                {
                    RestaurantId = existing.RestaurantId,
                    ProductId = id,
                    MovementType = delta > 0m ? StockMovementType.ManualIncrease : StockMovementType.ManualDecrease,
                    QuantityDelta = delta,
                    UnitCostSnapshot = existing.AverageCost > 0m ? existing.AverageCost : null,
                    TriggeredByImportId = null,
                    TriggeredByUserId = _currentUser.UserId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _stockMovementRepository.AddAsync(movement, cancellationToken);
            }
        }
        catch
        {
        }

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
            product.MaximumQuantity,
            product.ReorderPoint,
            product.AverageCost,
            product.MovingAverageConsumption,
            product.LastConsumptionRecalculatedAtUtc,
            product.IsActive,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
}
