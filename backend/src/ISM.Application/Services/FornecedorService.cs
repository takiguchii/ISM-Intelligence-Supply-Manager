using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services;

public sealed class FornecedorService : IFornecedorService
{
    private readonly IFornecedorRepository _fornecedorRepository;

    public FornecedorService(IFornecedorRepository fornecedorRepository)
    {
        _fornecedorRepository = fornecedorRepository;
    }

    public async Task<FornecedorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var fornecedor = await _fornecedorRepository.GetByIdAsync(id, cancellationToken);
        if (fornecedor == null)
            return null;

        return MapToDto(fornecedor);
    }

    public async Task<IReadOnlyList<FornecedorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var fornecedores = await _fornecedorRepository.GetAllAsync(cancellationToken);
        return fornecedores.Select(MapToDto).ToList();
    }

    public async Task<FornecedorDto> CreateAsync(FornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var fornecedor = new Fornecedor
        {
            Nome = dto.Nome,
            Categoria = dto.Categoria,
            Email = dto.Email,
            Telefone = dto.Telefone,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _fornecedorRepository.AddAsync(fornecedor, cancellationToken);
        await _fornecedorRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(fornecedor);
    }

    public async Task<bool> UpdateAsync(int id, FornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var fornecedor = await _fornecedorRepository.GetByIdAsync(id, cancellationToken);
        if (fornecedor == null)
            return false;

        fornecedor.Nome = dto.Nome;
        fornecedor.Categoria = dto.Categoria;
        fornecedor.Email = dto.Email;
        fornecedor.Telefone = dto.Telefone;
        fornecedor.UpdatedAtUtc = DateTime.UtcNow;

        _fornecedorRepository.Update(fornecedor);
        return await _fornecedorRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var fornecedor = await _fornecedorRepository.GetByIdAsync(id, cancellationToken);
        if (fornecedor == null)
            return false;

        _fornecedorRepository.Delete(fornecedor);
        return await _fornecedorRepository.SaveChangesAsync(cancellationToken);
    }

    private static FornecedorDto MapToDto(Fornecedor fornecedor)
    {
        return new FornecedorDto
        {
            Id = fornecedor.Id,
            Nome = fornecedor.Nome,
            Categoria = fornecedor.Categoria,
            Email = fornecedor.Email,
            Telefone = fornecedor.Telefone
        };
    }
}
