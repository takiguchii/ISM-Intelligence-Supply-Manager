namespace ISM.Domain.Modules.Orders;

public enum PaymentMethod
{
    Unspecified = 0,
    CreditCard = 1,
    DebitCard = 2,
    Pix = 3,
    Cash = 4,
    Voucher = 5,
    Other = 6
}
