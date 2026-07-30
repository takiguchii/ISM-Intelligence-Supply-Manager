using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface IProductService
{
    Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
