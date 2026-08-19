using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services;

public sealed class FornecedorService : IFornecedorService
{
    private readonly IFornecedorRepository _fornecedorRepository;
    private readonly ICurrentUser _currentUser;

    public FornecedorService(
        IFornecedorRepository fornecedorRepository,
        ICurrentUser currentUser)
    {
        _fornecedorRepository = fornecedorRepository;
        _currentUser = currentUser;
    }

    public async Task<FornecedorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var fornecedor = await _fornecedorRepository.GetByIdAsync(id, cancellationToken);
        if (fornecedor is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (fornecedor.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
        }

        return Map(fornecedor);
    }

    public async Task<IReadOnlyList<FornecedorResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var fornecedores = await _fornecedorRepository.GetAllAsync(cancellationToken);
        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            fornecedores = fornecedores.Where(f => f.RestaurantId == _currentUser.RestaurantId.Value).ToList();
        }
        return fornecedores.Select(Map).ToArray();
    }

    public async Task<FornecedorResponse> CreateAsync(CreateFornecedorRequest request, CancellationToken cancellationToken = default)
    {
        var restaurantId = ResolveRestaurantId(request.RestaurantId);

        if (!_currentUser.IsSuperAdmin && !_currentUser.IsManagerOrAbove)
            throw new UnauthorizedAccessException("Apenas gerentes podem cadastrar fornecedores.");

        var fornecedor = new Fornecedor
        {
            RestaurantId = restaurantId,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _fornecedorRepository.AddAsync(fornecedor, cancellationToken);
        return Map(fornecedor);
    }

    public async Task<FornecedorResponse?> UpdateAsync(int id, UpdateFornecedorRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _fornecedorRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem editar fornecedores.");
        }

        var updated = new Fornecedor
        {
            Id = id,
            RestaurantId = existing.RestaurantId,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            IsActive = existing.IsActive,
            CreatedAtUtc = existing.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _fornecedorRepository.UpdateAsync(updated, cancellationToken);
        return Map(updated);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _fornecedorRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null) return false;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return false;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem deletar fornecedores.");
        }

        return await _fornecedorRepository.DeleteAsync(id, cancellationToken);
    }

    private int ResolveRestaurantId(int requestRestaurantId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            if (requestRestaurantId <= 0)
                throw new InvalidOperationException("Super Admin deve informar o RestaurantId.");
            return requestRestaurantId;
        }

        if (!_currentUser.RestaurantId.HasValue)
            throw new UnauthorizedAccessException("Usuário não vinculado a restaurante.");

        if (requestRestaurantId > 0 && requestRestaurantId != _currentUser.RestaurantId.Value)
            throw new UnauthorizedAccessException("Você só pode cadastrar fornecedores no seu restaurante.");

        return _currentUser.RestaurantId.Value;
    }

    private static FornecedorResponse Map(Fornecedor fornecedor)
        => new(
            fornecedor.Id,
            fornecedor.RestaurantId,
            fornecedor.Name,
            fornecedor.Category,
            fornecedor.Description,
            fornecedor.Email,
            fornecedor.Phone,
            fornecedor.IsActive,
            fornecedor.CreatedAtUtc,
            fornecedor.UpdatedAtUtc);
}
