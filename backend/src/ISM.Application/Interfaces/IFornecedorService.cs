using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface IFornecedorService
{
    Task<FornecedorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FornecedorResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FornecedorResponse> CreateAsync(CreateFornecedorRequest request, CancellationToken cancellationToken = default);
    Task<FornecedorResponse?> UpdateAsync(int id, UpdateFornecedorRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
