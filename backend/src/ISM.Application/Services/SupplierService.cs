using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services;

public sealed class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ICurrentUser _currentUser;

    public SupplierService(
        ISupplierRepository supplierRepository,
        ICurrentUser currentUser)
    {
        _supplierRepository = supplierRepository;
        _currentUser = currentUser;
    }

    public async Task<SupplierResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (supplier.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
        }

        return Map(supplier);
    }

    public async Task<IReadOnlyList<SupplierResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);
        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            suppliers = suppliers.Where(f => f.RestaurantId == _currentUser.RestaurantId.Value).ToList();
        }
        return suppliers.Select(Map).ToArray();
    }

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var restaurantId = ResolveRestaurantId(request.RestaurantId);

        if (!_currentUser.IsSuperAdmin && !_currentUser.IsManagerOrAbove)
            throw new UnauthorizedAccessException("Apenas gerentes podem cadastrar fornecedores.");

        var supplier = new Supplier
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

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        return Map(supplier);
    }

    public async Task<SupplierResponse?> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem editar fornecedores.");
        }

        var updated = new Supplier
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

        await _supplierRepository.UpdateAsync(updated, cancellationToken);
        return Map(updated);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null) return false;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return false;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem deletar fornecedores.");
        }

        return await _supplierRepository.DeleteAsync(id, cancellationToken);
    }

    private int ResolveRestaurantId(int requestRestaurantId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            if (requestRestaurantId > 0)
                return requestRestaurantId;
            return 1; // Fallback para restaurante padrão em ambiente admin
        }

        if (!_currentUser.RestaurantId.HasValue)
            return 1; // Fallback seguro para o restaurante inicial

        if (requestRestaurantId > 0 && requestRestaurantId != _currentUser.RestaurantId.Value)
            throw new UnauthorizedAccessException("Você só pode cadastrar fornecedores no seu restaurante.");

        return _currentUser.RestaurantId.Value;
    }

    private static SupplierResponse Map(Supplier supplier)
        => new(
            supplier.Id,
            supplier.RestaurantId,
            supplier.Name,
            supplier.Category,
            supplier.Description,
            supplier.Email,
            supplier.Phone,
            supplier.IsActive,
            supplier.CreatedAtUtc,
            supplier.UpdatedAtUtc);
}
