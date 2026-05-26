using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories;

public sealed class FornecedorRepository : IFornecedorRepository
{
    private readonly IsmDbContext _dbContext;

    public FornecedorRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Fornecedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Fornecedores
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Fornecedor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Fornecedores.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        await _dbContext.Fornecedores.AddAsync(fornecedor, cancellationToken);
    }

    public void Update(Fornecedor fornecedor)
    {
        _dbContext.Fornecedores.Update(fornecedor);
    }

    public void Delete(Fornecedor fornecedor)
    {
        _dbContext.Fornecedores.Remove(fornecedor);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
