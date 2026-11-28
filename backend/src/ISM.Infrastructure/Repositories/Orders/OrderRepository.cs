using ISM.Domain.Interfaces;
using ISM.Application.DTOs.Orders;
using ISM.Application.Interfaces.Orders;
using ISM.Domain.Modules.Orders;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace ISM.Infrastructure.Repositories.Orders;

public sealed class OrderRepository(IsmDbContext context) : IOrderRepository, ITabStore
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> PaymentGates = new();
    public Task<Order?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default) =>
        context.Orders.Include(order => order.Items).FirstOrDefaultAsync(order => order.RestaurantId == restaurantId && order.Id == id, cancellationToken);

    public Task<Order?> GetByRequestIdAsync(int restaurantId, Guid requestId, CancellationToken cancellationToken = default) =>
        context.Orders.Include(order => order.Items).FirstOrDefaultAsync(order => order.RestaurantId == restaurantId && order.RequestId == requestId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await context.Orders.AsNoTracking().Include(order => order.Items)
            .Where(order => order.RestaurantId == restaurantId).OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetPageAsync(int restaurantId, int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = context.Orders.AsNoTracking().Include(order => order.Items)
            .Where(order => order.RestaurantId == restaurantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(order =>
                order.TableReference.Contains(term) ||
                order.Id.ToString().Contains(term) ||
                order.Items.Any(item => item.DishName.Contains(term)));
        }

        return await query
            .OrderByDescending(order => order.CreatedAtUtc).ThenByDescending(order => order.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(int restaurantId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = context.Orders.Where(order => order.RestaurantId == restaurantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(order =>
                order.TableReference.Contains(term) ||
                order.Id.ToString().Contains(term) ||
                order.Items.Any(item => item.DishName.Contains(term)));
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        context.Orders.Add(order);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return order;
        }
        catch (DbUpdateException)
        {
            foreach (var item in order.Items) context.Entry(item).State = EntityState.Detached;
            context.Entry(order).State = EntityState.Detached;
            var existing = await GetByRequestIdAsync(order.RestaurantId, order.RequestId, cancellationToken);
            if (existing is null) throw;
            return existing;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException("O pedido foi alterado. Atualize a tela e tente novamente.", exception);
        }
    }

    public async Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
    {
        context.Orders.Remove(order);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OpenTabResponse?> GetOpenAsync(int restaurantId, string tableReference, CancellationToken cancellationToken)
    {
        var reference = tableReference.Trim();
        var tab = await context.Tabs.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.RestaurantId == restaurantId && candidate.OpenTableReference == reference, cancellationToken);
        if (tab is not null)
        {
            var existingOrders = await GetTabOrders(restaurantId, reference, cancellationToken);
            return ToOpenTab(tab, existingOrders);
        }

        var orders = await GetTabOrders(restaurantId, reference, cancellationToken);
        if (orders.Count == 0) return null;

        tab = new Tab
        {
            RestaurantId = restaurantId,
            TableReference = reference,
            OpenTableReference = reference,
            Total = orders.Where(order => order.Status == OrderStatus.Completed).Sum(Total),
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Tabs.Add(tab);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.Entry(tab).State = EntityState.Detached;
            tab = await context.Tabs.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.RestaurantId == restaurantId && candidate.OpenTableReference == reference, cancellationToken);
            if (tab is null) throw;
        }
        return ToOpenTab(tab, orders);
    }

    public async Task<IReadOnlyList<OpenTabResponse>> GetOpenTabsAsync(int restaurantId, CancellationToken cancellationToken)
    {
        var tables = await context.Orders.AsNoTracking()
            .Where(order => order.RestaurantId == restaurantId && order.PaidTabId == null && order.Status != OrderStatus.Cancelled)
            .Select(order => order.TableReference).Distinct().ToListAsync(cancellationToken);
        var result = new List<OpenTabResponse>(tables.Count);
        foreach (var table in tables)
        {
            var tab = await GetOpenAsync(restaurantId, table, cancellationToken);
            if (tab is not null) result.Add(tab);
        }
        return result.OrderBy(tab => tab.TableReference, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<TabPaymentResponse?> PayAsync(int restaurantId, int tabId, PayTabRequest request, CancellationToken cancellationToken)
    {
        var gate = PaymentGates.GetOrAdd($"{restaurantId}:{tabId}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(() =>
            {
                // A retry must start from a clean tracker and repeat the complete unit of work.
                context.ChangeTracker.Clear();
                return PayCoreAsync(restaurantId, tabId, request, cancellationToken);
            });
        }
        finally { gate.Release(); }
    }

    private async Task<TabPaymentResponse?> PayCoreAsync(int restaurantId, int tabId, PayTabRequest request, CancellationToken cancellationToken)
    {
        var tab = await context.Tabs.FirstOrDefaultAsync(candidate => candidate.RestaurantId == restaurantId && candidate.Id == tabId, cancellationToken);
        if (tab is null) return null;
        if (tab.PaidAtUtc.HasValue)
        {
            if (tab.IdempotencyKey == request.IdempotencyKey && tab.PaymentMethod == request.PaymentMethod && tab.Total == request.Total)
                return ToPayment(tab);
            if (tab.IdempotencyKey == request.IdempotencyKey)
                throw new InvalidOperationException("A chave de idempotência foi reutilizada com dados diferentes.");
            throw new TabPaymentConflictException("A comanda já foi fechada com outra operação.");
        }

        var reusedKey = await context.Tabs.AsNoTracking().AnyAsync(candidate => candidate.RestaurantId == restaurantId && candidate.Id != tabId && candidate.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (reusedKey) throw new InvalidOperationException("A chave de idempotência já foi usada em outra comanda.");

        var orders = await GetTabOrders(restaurantId, tab.TableReference, cancellationToken);
        var pending = orders.Any(order => order.Status is OrderStatus.Submitted or OrderStatus.Preparing or OrderStatus.Ready);
        if (pending) throw new TabPaymentConflictException("A comanda possui pedidos ainda em preparo.");
        var payable = orders.Where(order => order.Status == OrderStatus.Completed).ToArray();
        if (payable.Length == 0) return null;
        var total = payable.Sum(Total);
        if (request.Total != total) throw new InvalidOperationException("O total da comanda mudou. Atualize o caixa antes de receber.");

        var now = DateTime.UtcNow;
        tab.IdempotencyKey = request.IdempotencyKey;
        tab.PaymentMethod = request.PaymentMethod;
        tab.Total = total;
        tab.PaidAtUtc = now;
        tab.OpenTableReference = null;
        tab.UpdatedAtUtc = now;
        foreach (var order in payable)
        {
            order.Status = OrderStatus.Paid;
            order.PaymentMethod = request.PaymentMethod;
            order.PaidTabId = tab.Id;
            order.PaidAtUtc = now;
            order.UpdatedAtUtc = now;
            order.Version++;
        }

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(global::System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            var existing = await context.Tabs.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.RestaurantId == restaurantId && candidate.Id == tabId, cancellationToken);
            if (existing?.PaidAtUtc is not null && existing.IdempotencyKey == request.IdempotencyKey && existing.PaymentMethod == request.PaymentMethod && existing.Total == request.Total)
                return ToPayment(existing);
            throw new TabPaymentConflictException("A comanda foi recebida simultaneamente em outro caixa.");
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            var existing = await context.Tabs.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.RestaurantId == restaurantId && candidate.Id == tabId, cancellationToken);
            if (existing?.PaidAtUtc is not null && existing.IdempotencyKey == request.IdempotencyKey) return ToPayment(existing);
            throw new TabPaymentConflictException("A comanda foi recebida simultaneamente em outro caixa.");
        }
        return ToPayment(tab);
    }

    private async Task<List<Order>> GetTabOrders(int restaurantId, string tableReference, CancellationToken cancellationToken) =>
        await context.Orders.Include(order => order.Items)
            .Where(order => order.RestaurantId == restaurantId && order.TableReference == tableReference && order.PaidTabId == null && order.Status != OrderStatus.Cancelled)
            .OrderBy(order => order.CreatedAtUtc).ThenBy(order => order.Id).ToListAsync(cancellationToken);

    private static decimal Total(Order order) => order.Items.Sum(item => item.UnitPrice * item.Quantity);
    private static OpenTabResponse ToOpenTab(Tab tab, IReadOnlyCollection<Order> orders) =>
        new(tab.Id, tab.TableReference, orders.Where(order => order.Status == OrderStatus.Completed).Sum(Total),
            orders.All(order => order.Status == OrderStatus.Completed), orders.Select(order => order.Id).ToArray());
    private static TabPaymentResponse ToPayment(Tab tab) => new(tab.Id, tab.PaymentMethod, tab.Total, tab.PaidAtUtc!.Value);
}
