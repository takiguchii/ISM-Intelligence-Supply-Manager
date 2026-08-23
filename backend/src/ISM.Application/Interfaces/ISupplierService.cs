using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface ISupplierService
{
    Task<SupplierResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierResponse?> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
