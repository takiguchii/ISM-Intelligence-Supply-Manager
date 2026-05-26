using ISM.Domain.Entities;

namespace ISM.Domain.Interfaces;

public interface IFornecedorRepository
{
    Task<Fornecedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fornecedor>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    void Update(Fornecedor fornecedor);
    void Delete(Fornecedor fornecedor);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
