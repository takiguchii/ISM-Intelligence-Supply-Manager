using ISM.Domain.Entities;

namespace ISM.Domain.Interfaces;

public interface IFornecedorRepository
{
    Task<Fornecedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fornecedor>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Fornecedor> AddAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    Task<Fornecedor> UpdateAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
