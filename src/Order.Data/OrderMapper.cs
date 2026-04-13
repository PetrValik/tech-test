using Order.Model;
using System;
using System.Linq;

namespace Order.Data;

/// <summary>
/// Maps <c>Order.Data.Entities</c> objects to <c>Order.Model</c> view models.
/// All members are pure functions with no side-effects.
/// </summary>
internal static class OrderMapper
{
    /// <summary>
    /// Maps an <see cref="Entities.Order"/> entity (with Status and Items.Product loaded) to
    /// an <see cref="OrderSummary"/> model.
    /// </summary>
    internal static OrderSummary ToOrderSummary(Entities.Order order) =>
        new OrderSummary
        {
            Id         = new Guid(order.Id),
            ResellerId = new Guid(order.ResellerId),
            CustomerId = new Guid(order.CustomerId),
            StatusId   = new Guid(order.StatusId),
            StatusName = order.Status.Name,
            ItemCount  = order.Items.Count,
            TotalCost  = order.Items.Sum(item => item.Quantity * item.Product.UnitCost),
            TotalPrice = order.Items.Sum(item => item.Quantity * item.Product.UnitPrice),
            CreatedDate = order.CreatedDate
        };

    /// <summary>
    /// Maps an <see cref="Entities.Order"/> entity (with Status, Items.Service, and Items.Product
    /// loaded) to an <see cref="OrderDetail"/> model.
    /// </summary>
    internal static OrderDetail ToOrderDetail(Entities.Order entity) =>
        new OrderDetail
        {
            Id         = new Guid(entity.Id),
            ResellerId = new Guid(entity.ResellerId),
            CustomerId = new Guid(entity.CustomerId),
            StatusId   = new Guid(entity.StatusId),
            StatusName = entity.Status.Name,
            CreatedDate = entity.CreatedDate,
            TotalCost  = entity.Items.Sum(item => item.Quantity * item.Product.UnitCost),
            TotalPrice = entity.Items.Sum(item => item.Quantity * item.Product.UnitPrice),
            Items      = entity.Items.Select(ToOrderItem).ToList()
        };

    /// <summary>
    /// Maps an <see cref="Entities.OrderItem"/> entity (with Service and Product loaded) to
    /// an <see cref="Model.OrderItem"/> model.
    /// </summary>
    internal static Model.OrderItem ToOrderItem(Entities.OrderItem item) =>
        new Model.OrderItem
        {
            Id          = new Guid(item.Id),
            OrderId     = new Guid(item.OrderId),
            ServiceId   = new Guid(item.ServiceId),
            ServiceName = item.Service.Name,
            ProductId   = new Guid(item.ProductId),
            ProductName = item.Product.Name,
            UnitCost    = item.Product.UnitCost,
            UnitPrice   = item.Product.UnitPrice,
            TotalCost   = item.Product.UnitCost * item.Quantity,
            TotalPrice  = item.Product.UnitPrice * item.Quantity,
            Quantity    = item.Quantity
        };

    /// <summary>
    /// Calculates the profit for a single order item: (UnitPrice − UnitCost) × Quantity.
    /// Used for client-side aggregation only; the equivalent inline expression is required
    /// for server-side (EF Core cannot translate a method call to SQL).
    /// </summary>
    internal static decimal CalculateItemProfit(Entities.OrderItem item) =>
        item.Quantity * (item.Product.UnitPrice - item.Product.UnitCost);
}
