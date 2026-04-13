using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Features.Orders.GetOrders;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.SearchOrders;

/// <summary>
/// Builds a server-side SQL projection from the non-null filter parameters.
/// A single unified query path handles all combinations including MinTotal/MaxTotal filters.
/// </summary>
/// <param name="orderContext">The database context used to query and filter orders.</param>
public sealed class SearchOrdersHandler(OrderContext orderContext): IRequestHandler<SearchOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    /// <summary>
    /// Applies each non-null filter predicate server-side, then pages and returns the matching orders.
    /// </summary>
    /// <param name="request">The search query containing filter criteria and pagination parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="PagedResult{T}"/> of <see cref="OrderSummaryResponse"/> records matching all specified filters.</returns>
    public async Task<PagedResult<OrderSummaryResponse>> Handle(SearchOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = BuildBaseQuery(request);

        var projectedQuery = query.Select(order => new
        {
            order.Id,
            order.ResellerId,
            order.CustomerId,
            order.StatusId,
            StatusName = order.Status.Name,
            order.CreatedDate,
            ItemCount = order.Items.Count(),
            TotalCost = order.Items.Sum(item => (decimal)(item.Quantity ?? 0) * item.Product.UnitCost),
            TotalPrice = order.Items.Sum(item => (decimal)(item.Quantity ?? 0) * item.Product.UnitPrice)
        });

        if (request.MinTotal.HasValue)
        {
            projectedQuery = projectedQuery.Where(row => row.TotalPrice >= request.MinTotal.Value);
        }

        if (request.MaxTotal.HasValue)
        {
            projectedQuery = projectedQuery.Where(row => row.TotalPrice <= request.MaxTotal.Value);
        }

        var totalCount = await projectedQuery.CountAsync(cancellationToken);

        var pageRows = await projectedQuery
            .OrderByDescending(row => row.CreatedDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var page = pageRows.Select(row => new OrderSummaryResponse(
            new Guid(row.Id),
            new Guid(row.ResellerId),
            new Guid(row.CustomerId),
            new Guid(row.StatusId),
            row.StatusName,
            row.CreatedDate,
            row.ItemCount,
            row.TotalCost,
            row.TotalPrice)).ToList();

        return new PagedResult<OrderSummaryResponse>(page, totalCount, request.Page, request.PageSize);
    }

    /// <summary>
    /// Constructs the base <see cref="IQueryable{T}"/> by applying all non-null
    /// date, reseller, customer, and status filters from <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The search query whose non-null filter properties are applied as WHERE clauses.</param>
    /// <returns>An <see cref="IQueryable{T}"/> of <see cref="Infrastructure.Entities.Order"/> entities filtered to the requested criteria.</returns>
    private IQueryable<Infrastructure.Entities.Order> BuildBaseQuery(SearchOrdersQuery request)
    {
        var query = orderContext.Orders.AsNoTracking();

        if (request.From.HasValue)
        {
            query = query.Where(order => order.CreatedDate >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(order => order.CreatedDate <= request.To.Value);
        }

        if (request.ResellerId.HasValue)
        {
            var resellerBytes = request.ResellerId.Value.ToByteArray();
            query = query.Where(order => order.ResellerId == resellerBytes);
        }

        if (request.CustomerId.HasValue)
        {
            var customerBytes = request.CustomerId.Value.ToByteArray();
            query = query.Where(order => order.CustomerId == customerBytes);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var canonicalStatus = OrderStatusNames.All.FirstOrDefault(statusName =>
                string.Equals(statusName, request.Status, StringComparison.OrdinalIgnoreCase));
            if (canonicalStatus is not null)
            {
                query = query.Where(order => order.Status.Name == canonicalStatus);
            }
        }

        return query;
    }
}
