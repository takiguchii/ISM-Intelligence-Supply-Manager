using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Orders;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Orders;

public sealed class OrderRepository(IsmDbContext context) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default) =>
        context.Orders.Include(order => order.Items).FirstOrDefaultAsync(order => order.RestaurantId == restaurantId && order.Id == id, cancellationToken);

    public Task<Order?> GetByRequestIdAsync(int restaurantId, Guid requestId, CancellationToken cancellationToken = default) =>
        context.Orders.Include(order => order.Items).FirstOrDefaultAsync(order => order.RestaurantId == restaurantId && order.RequestId == requestId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await context.Orders.AsNoTracking().Include(order => order.Items)
            .Where(order => order.RestaurantId == restaurantId).OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

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
}
