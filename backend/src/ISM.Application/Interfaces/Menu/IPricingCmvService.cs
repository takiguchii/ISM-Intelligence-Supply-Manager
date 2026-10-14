using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;


public interface IPricingCmvService
{
    Task<IReadOnlyList<MarginAlertResponse>> RecalculateForProductAsync(int productId, CancellationToken cancellationToken = default); //função para recalcular o preço de um prato a partir de seus produtos
}