using System.Text.RegularExpressions;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Entities;
using ISM.Domain.Modules.DataImport;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.DataImport;

/// <summary>Importador de fornecedores (planilha/CSV → Supplier).</summary>
public sealed class CsvSupplierImporter : CsvImporterBase
{
    public override TargetImportEntity HandlesEntity => TargetImportEntity.Supplier;

    private readonly ISupplierRepository _supplierRepo;
    private Dictionary<string, Supplier>? _existingByName;

    public CsvSupplierImporter(ISupplierRepository supplierRepo, IImportAuditRepository auditRepo)
        : base(auditRepo)
        => _supplierRepo = supplierRepo;

    protected override async Task OnBeforeRowsAsync(ImportContext context, CancellationToken ct)
    {
        _existingByName = (await _supplierRepo.GetAllAsync(ct))
            .Where(f => f.RestaurantId == context.RestaurantId)
            .GroupBy(f => f.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    protected override async Task<int> ImportRowAsync(
        Dictionary<string, string> row,
        ImportContext context,
        int rowNumber,
        CancellationToken ct)
    {
        var (name, category, description, email, phone) = NormalizeRow(row, rowNumber);
        var key = name.Trim();

        Supplier entity;
        if (context.UpsertStrategy == UpsertStrategy.MergeByNameAndRestaurant &&
            _existingByName!.TryGetValue(key, out var existing))
        {
            existing.Category = category;
            existing.Description = description;
            existing.Email = email;
            existing.Phone = phone;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            entity = await _supplierRepo.UpdateAsync(existing, ct);
        }
        else
        {
            entity = new Supplier
            {
                RestaurantId = context.RestaurantId,
                Name = name.Trim(),
                Category = category,
                Description = description,
                Email = email,
                Phone = phone,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            entity = await _supplierRepo.AddAsync(entity, ct);
        }

        _existingByName![key] = entity;
        return entity.Id;
    }

    protected override string? GetRowKeyValue(Dictionary<string, string> row) =>
        row.TryGetValue("Nome", out var n) ? n :
        row.TryGetValue("Name", out var ne) ? ne :
        row.TryGetValue("Fornecedor", out var f) ? f :
        row.TryGetValue("Supplier", out var s) ? s : null;

    private static (string name, string category, string? description, string email, string phone)
        NormalizeRow(Dictionary<string, string> row, int rowNumber)
    {
        string Get(params string[] keys)
        {
            foreach (var k in keys)
                if (row.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            return string.Empty;
        }

        var name = Get("Nome", "Name", "Fornecedor", "Supplier");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"Nome do fornecedor é obrigatório na linha {rowNumber}.");

        var category = Get("Categoria", "Category", "Ramo");
        if (string.IsNullOrWhiteSpace(category)) category = "Geral";

        var description = Get("Descricao", "Description", "Descrição", "Observacao");
        if (string.IsNullOrWhiteSpace(description)) description = null;

        var email = Get("Email", "E-mail", "emailContato");
        if (string.IsNullOrWhiteSpace(email)) email = "nao-informado@exemplo.com";
        else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new InvalidOperationException($"Email inválido '{email}' na linha {rowNumber}.");

        var phone = Get("Telefone", "Phone", "Tel", "Contato");
        if (string.IsNullOrWhiteSpace(phone)) phone = "(00) 00000-0000";

        return (name, category, description, email, phone);
    }
}
