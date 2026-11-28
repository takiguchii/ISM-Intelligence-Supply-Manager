using ISM.Domain.Modules.Orders;

namespace ISM.Application.DTOs.Orders;

public sealed record OpenTabResponse(int Id, string TableReference, decimal Total, bool CanPay, IReadOnlyList<int> OrderIds);
public sealed record PayTabRequest(Guid IdempotencyKey, PaymentMethod PaymentMethod, decimal Total);
public sealed record TabPaymentResponse(int TabId, PaymentMethod PaymentMethod, decimal Total, DateTime PaidAtUtc);
