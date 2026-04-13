using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.GetOrderById;

/// <summary>
/// Returns the full detail of a single order identified by its GUID.
/// </summary>
/// <param name="orderContext">The database context used to query the order with all related entities.</param>
public sealed class GetOrderByIdHandler(OrderContext orderContext)
    : IRequestHandler<GetOrderByIdQuery, OrderDetailResponse?>
{
    /// <summary>
    /// Returns the full detail of a single order by its GUID, including all line items and computed totals.
    /// Returns <see langword="null"/> if the order does not exist.
    /// </summary>
    /// <param name="query">The query containing the order GUID to look up.</param>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing an <see cref="OrderDetailResponse"/> if the order exists,
    /// or <see langword="null"/> if it was not found.
    /// </returns>
    public async Task<OrderDetailResponse?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var orderIdBytes = query.OrderId.ToByteArray();

        var order = await orderContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderIdBytes)
            .Include(order => order.Status)
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .Include(order => order.Items)
                .ThenInclude(item => item.Service)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return null;
        }

        var itemResponses = order.Items.Select(MapItemToResponse).ToList();

        return MapOrderToDetailResponse(order, itemResponses);
    }

    /// <summary>
    /// Maps a single <see cref="Infrastructure.Entities.OrderItem"/> entity to its API response model.
    /// Computes total cost and total price from unit values multiplied by quantity.
    /// </summary>
    /// <param name="item">The order item entity to map.</param>
    /// <returns>An <see cref="OrderItemResponse"/> populated with product details and computed totals.</returns>
    private static OrderItemResponse MapItemToResponse(Infrastructure.Entities.OrderItem item)
    {
        var quantity = item.Quantity ?? 0;
        return new OrderItemResponse(
            new Guid(item.Id),
            new Guid(item.OrderId),
            new Guid(item.ServiceId),
            item.Service.Name,
            new Guid(item.ProductId),
            item.Product.Name,
            quantity,
            item.Product.UnitCost,
            item.Product.UnitPrice,
            item.Product.UnitCost * quantity,
            item.Product.UnitPrice * quantity);
    }

    /// <summary>
    /// Constructs the full <see cref="OrderDetailResponse"/> from the order entity and its already-mapped item responses.
    /// Sums item-level costs and prices to produce the order-level totals.
    /// </summary>
    /// <param name="order">The order entity loaded from the database.</param>
    /// <param name="itemResponses">The pre-mapped collection of order item response models.</param>
    /// <returns>A fully populated <see cref="OrderDetailResponse"/>.</returns>
    private static OrderDetailResponse MapOrderToDetailResponse(
        Infrastructure.Entities.Order order,
        IReadOnlyList<OrderItemResponse> itemResponses) =>
        new OrderDetailResponse(
            new Guid(order.Id),
            new Guid(order.ResellerId),
            new Guid(order.CustomerId),
            new Guid(order.StatusId),
            order.Status.Name,
            order.CreatedDate,
            itemResponses.Sum(item => item.TotalCost),
            itemResponses.Sum(item => item.TotalPrice),
            itemResponses,
            order.ConcurrencyStamp);
}
