using Microsoft.EntityFrameworkCore;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Order.Data.Repositories;

/// <summary>
/// EF Core implementation of IOrderRepository.
/// All aggregate queries use explicit Include / ThenInclude loading; lazy loading is not enabled.
/// </summary>
public class OrderRepository : IOrderRepository
{
    /// <summary>
    /// The EF Core context instance used for all data access. The context is registered as scoped in the DI container,
    /// </summary>
    private readonly OrderContext _orderContext;

    /// <summary>
    /// Creates a new repository instance backed by the supplied context.
    /// </summary>
    /// <param name="orderContext">The scoped EF Core context injected by the DI container.</param>
    public OrderRepository(OrderContext orderContext)
    {
        _orderContext = orderContext;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Totals (TotalCost, TotalPrice) are computed client-side after eager loading to remain
    /// compatible with all EF Core providers. An overflow guard caps the skip value to
    /// prevent arithmetic overflow on pathological page numbers.
    /// </remarks>
    public async Task<PagedResult<OrderSummary>> GetOrdersAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagingHelper.NormalizePage(page, pageSize);
        if (PagingHelper.IsPageOverflow(page))
        {
            return PagingHelper.EmptyPagedResult(page, pageSize);
        }

        return await FetchPagedOrdersAsync(_orderContext.Orders, page, pageSize, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses <see cref="Enumerable.SequenceEqual{T}(IEnumerable{T}, IEnumerable{T})"/> for
    /// the InMemory provider (which cannot translate binary equality) and direct byte-array
    /// equality for MySQL and SQLite.
    /// </remarks>
    public async Task<OrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var orderIdBytes = orderId.ToByteArray();

        var entity = await WhereByteId(_orderContext.Orders, order => order.Id, orderIdBytes)
            .AsNoTracking()
            .Include(order => order.Status)
            .Include(order => order.Items)
                .ThenInclude(item => item.Service)
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return OrderMapper.ToOrderDetail(entity);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<OrderSummary>> GetOrdersByStatusAsync(string statusName, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagingHelper.NormalizePage(page, pageSize);
        if (PagingHelper.IsPageOverflow(page))
        {
            return PagingHelper.EmptyPagedResult(page, pageSize);
        }

        var status = await FindStatusByNameAsync(statusName, cancellationToken);
        if (status == null)
        {
            return PagingHelper.EmptyPagedResult(page, pageSize);
        }

        var statusId = status.Id;
        var filteredQuery = WhereByteId(_orderContext.Orders, order => order.StatusId, statusId);

        return await FetchPagedOrdersAsync(filteredQuery, page, pageSize, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// SQLite and the InMemory provider do not support server-side <c>GroupBy</c> on navigation
    /// property keys, so those providers fall back to client-side aggregation. MySQL executes
    /// the full aggregation server-side via an EF Core GroupBy projection.
    /// </remarks>
    public async Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync(CancellationToken cancellationToken = default)
    {
        if (_orderContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true
            || _orderContext.Database.IsInMemory())
        {
            return await GetMonthlyProfitClientSideAsync(cancellationToken);
        }

        return await GetMonthlyProfitServerSideAsync(cancellationToken);
    }

    /// <summary>
    /// Client-side aggregation for SQLite and InMemory providers, which do not support
    /// server-side GroupBy on navigation property keys.
    /// </summary>
    private async Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitClientSideAsync(CancellationToken cancellationToken)
    {
        var items = await _orderContext.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.Status.Name == OrderStatusNames.Completed)
            .Include(item => item.Product)
            .Include(item => item.Order)
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(item => new { item.Order.CreatedDate.Year, item.Order.CreatedDate.Month })
            .Select(group => new MonthlyProfit
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                TotalProfit = group.Sum(OrderMapper.CalculateItemProfit)
            })
            .OrderBy(profit => profit.Year)
            .ThenBy(profit => profit.Month)
            .ToList();
    }

    /// <summary>
    /// Server-side aggregation for MySQL — computes profit via EF Core GroupBy projection.
    /// The profit formula must stay inline; EF Core cannot translate a C# helper call to SQL.
    /// </summary>
    private async Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitServerSideAsync(CancellationToken cancellationToken)
    {
        return await _orderContext.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.Status.Name == OrderStatusNames.Completed)
            .GroupBy(item => new { item.Order.CreatedDate.Year, item.Order.CreatedDate.Month })
            .Select(group => new MonthlyProfit
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                TotalProfit = group.Sum(item => item.Quantity * (item.Product.UnitPrice - item.Product.UnitCost))
            })
            .OrderBy(profit => profit.Year)
            .ThenBy(profit => profit.Month)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entities.OrderStatus?> FindStatusByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _orderContext.OrderStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(status => status.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses <see cref="Enumerable.SequenceEqual{T}(IEnumerable{T}, IEnumerable{T})"/> for
    /// the InMemory provider and direct byte-array equality for MySQL and SQLite.
    /// The returned entity is change-tracked by the underlying <see cref="OrderContext"/>.
    /// </remarks>
    public async Task<Entities.Order?> FindOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var orderIdBytes = orderId.ToByteArray();

        return await WhereByteId(_orderContext.Orders, order => order.Id, orderIdBytes)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The InMemory provider cannot translate binary-column equality in a LINQ <c>Where</c> clause,
    /// so all products are loaded client-side and filtered with <see cref="Enumerable.SequenceEqual{T}"/>.
    /// MySQL and SQLite support the binary <c>Contains</c> predicate server-side.
    /// </remarks>
    public async Task<List<Entities.OrderProduct>> FindProductsByIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var productIdBytesList = productIds.Select(id => id.ToByteArray()).ToList();

        if (_orderContext.Database.IsInMemory())
        {
            // InMemory provider cannot translate byte-array equality — filter client-side.
            var allProducts = await _orderContext.OrderProducts.AsNoTracking().ToListAsync(cancellationToken);
            return allProducts
                .Where(product => productIdBytesList.Any(idBytes => product.Id.SequenceEqual(idBytes)))
                .ToList();
        }

        // MySQL / SQLite can compare binary columns directly in a WHERE clause.
        return await _orderContext.OrderProducts
            .AsNoTracking()
            .Where(product => productIdBytesList.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Both the order and its items are inserted inside a single database transaction
    /// so the write is atomic — either all rows are committed or none are.
    /// </remarks>
    public async Task SaveOrderAsync(Entities.Order order, IEnumerable<Entities.OrderItem> items, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _orderContext.Database.BeginTransactionAsync(cancellationToken);
        _orderContext.Orders.Add(order);
        _orderContext.OrderItems.AddRange(items);
        await _orderContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateOrderAsync(CancellationToken cancellationToken = default)
    {
        await _orderContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Executes count and fetch queries against <paramref name="filteredQuery"/>,
    /// applies standard includes and ordering, and returns a paged result.
    /// </summary>
    private async Task<PagedResult<OrderSummary>> FetchPagedOrdersAsync(
        IQueryable<Entities.Order> filteredQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var orders = await filteredQuery
            .AsNoTracking()
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .Include(order => order.Status)
            .OrderByDescending(order => order.CreatedDate)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummary>
        {
            Items = orders.Select(OrderMapper.ToOrderSummary).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Filters <paramref name="query"/> to rows whose <paramref name="selector"/> column equals <paramref name="idBytes"/>.
    /// Uses <see cref="Enumerable.SequenceEqual{T}(IEnumerable{T}, IEnumerable{T})"/> for the InMemory provider
    /// (which cannot translate binary equality) and direct byte-array equality for SQL providers.
    /// </summary>
    /// <param name="query">The base queryable to filter.</param>
    /// <param name="selector">Expression that selects the binary ID column to match against.</param>
    /// <param name="idBytes">The target ID as a byte array.</param>
    /// <returns>The filtered queryable.</returns>
    private IQueryable<Entities.Order> WhereByteId(
        IQueryable<Entities.Order> query,
        Expression<Func<Entities.Order, byte[]>> selector,
        byte[] idBytes)
    {
        if (_orderContext.Database.IsInMemory())
        {
            var column = selector.Compile();
            return query.Where(order => column(order).SequenceEqual(idBytes));
        }

        // Build `order => selector(order) == idBytes` as an expression tree so EF Core
        // can translate it to a SQL binary equality predicate.
        var parameter = selector.Parameters[0];
        var equality = Expression.Equal(selector.Body, Expression.Constant(idBytes));
        var predicate = Expression.Lambda<Func<Entities.Order, bool>>(equality, parameter);
        return query.Where(predicate);
    }
}
