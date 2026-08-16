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
        => await _dbContext.Fornecedores
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Fornecedor>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Fornecedores
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

    public async Task<Fornecedor> AddAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        _dbContext.Fornecedores.Add(fornecedor);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return fornecedor;
    }

    public async Task<Fornecedor> UpdateAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        _dbContext.Fornecedores.Update(fornecedor);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return fornecedor;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Fornecedores.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entity is null)
            return false;

        _dbContext.Fornecedores.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
