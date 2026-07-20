using ISM.Application.Interfaces;
using ISM.Application.DTOs;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Interfaces;

namespace ISM.Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : Map(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Select(Map).ToArray();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            CurrentQuantity = request.CurrentQuantity,
            MinimumQuantity = request.MinimumQuantity,
            AverageCost = request.AverageCost,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product, cancellationToken);
        return Map(product);
    }

    public async Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new Product
        {
            Id = id,
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            CurrentQuantity = request.CurrentQuantity,
            MinimumQuantity = request.MinimumQuantity,
            AverageCost = request.AverageCost,
            CreatedAtUtc = existing.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _productRepository.UpdateAsync(updated, cancellationToken);
        return Map(updated);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        => _productRepository.DeleteAsync(id, cancellationToken);

    private static ProductResponse Map(Product product)
        => new(
            product.Id,
            product.Name,
            product.Unit,
            product.CurrentQuantity,
            product.MinimumQuantity,
            product.AverageCost,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
}
