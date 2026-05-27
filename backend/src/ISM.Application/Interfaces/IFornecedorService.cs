using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface IFornecedorService
{
    Task<FornecedorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FornecedorDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FornecedorDto> CreateAsync(FornecedorDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, FornecedorDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
