using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record NotificationPreferences(
    bool Stock = true, bool Suppliers = true, bool Menu = true,
    bool EmailEnabled = false, bool WhatsAppEnabled = false,
    [EmailAddress, StringLength(150)] string? EmailAddress = null,
    [RegularExpression(@"^\+[1-9]\d{7,14}$", ErrorMessage = "Informe o WhatsApp com +, país e DDD, por exemplo +5511999999999.")] string? WhatsAppPhone = null);
