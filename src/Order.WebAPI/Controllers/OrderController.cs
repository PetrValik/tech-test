using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Order.Model;
using Order.Service;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Order.WebAPI.Controllers;

/// <summary>
/// Manages orders – create, query, update status, and report monthly profit.
/// </summary>
[Authorize]
[ApiController]
[Route("orders")]
[EnableRateLimiting("fixed")]
public class OrderController : ControllerBase
{
    /// <summary>
    /// Default page size for paginated endpoints.
    /// </summary>
    private const int DefaultPageSize = 50;

    /// <summary>
    /// The business-logic layer for orders, injected via constructor by the DI container.
    /// </summary>
    private readonly IOrderService _orderService;

    /// <summary>
    /// Creates a new controller with the injected order service.
    /// </summary>
    /// <param name="orderService">The business-logic layer injected by the DI container.</param>
    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Returns a summary list of all orders, ordered by creation date (newest first).
    /// </summary>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Number of items per page (default 50, max 200).</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>200 OK with a paginated list of order summaries.</returns>
    /// <response code="200">A paginated list of order summaries.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetOrdersAsync(page, pageSize, cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// Returns the full detail of a single order, including all items.
    /// </summary>
    /// <param name="orderId">The unique identifier of the order.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>200 OK with the order detail, or 404 Not Found if the order does not exist.</returns>
    /// <response code="200">The full order detail including all line items.</response>
    /// <response code="404">No order with the supplied ID was found.</response>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        return order != null ? Ok(order) : NotFound();
    }

    /// <summary>
    /// Returns all orders with the specified status name (e.g. "Created", "Completed").
    /// </summary>
    /// <param name="statusName">The exact order status name to filter by.</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Number of items per page (default 50, max 200).</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>200 OK with a paginated list of matching order summaries, or 400 Bad Request if the status name is empty.</returns>
    /// <response code="200">A paginated list of order summaries filtered by status.</response>
    /// <response code="400">The status name was null or whitespace.</response>
    [HttpGet("status/{statusName:maxlength(50)}")]
    [ProducesResponseType(typeof(PagedResult<OrderSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(
        string statusName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return BadRequest(new { error = "Status name is required." });
        }

        var orders = await _orderService.GetOrdersByStatusAsync(statusName, page, pageSize, cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// Updates the status of an existing order.
    /// </summary>
    /// <param name="orderId">The unique identifier of the order to update.</param>
    /// <param name="request">The new status name.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>
    /// 204 No Content on success; 400 Bad Request for an invalid status name or missing body;
    /// 404 Not Found if the order does not exist; 409 Conflict on a concurrent modification.
    /// </returns>
    /// <response code="204">The order status was updated successfully.</response>
    /// <response code="400">The request body was missing or the status name is not valid.</response>
    /// <response code="404">No order with the supplied ID was found.</response>
    /// <response code="409">The order was modified by another request concurrently — caller should retry.</response>
    [HttpPatch("{orderId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        var result = await _orderService.UpdateOrderStatusAsync(orderId, request.StatusName, cancellationToken);
        return result switch
        {
            UpdateOrderStatusResult.Success      => NoContent(),
            UpdateOrderStatusResult.OrderNotFound => NotFound(),
            UpdateOrderStatusResult.InvalidStatus => BadRequest(new { error = $"Invalid status: {request.StatusName}" }),
            UpdateOrderStatusResult.ConcurrencyConflict => Conflict(new { error = "The order was modified by another request. Please retry." }),
            _                                    => StatusCode(500, new { error = "An unexpected error occurred." })
        };
    }

    /// <summary>
    /// Creates a new order with one or more items.
    /// </summary>
    /// <param name="request">The reseller ID, customer ID, and list of product line items.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>201 Created with the new order ID, or 400 Bad Request if any product IDs are invalid or the body is missing.</returns>
    /// <response code="201">The order was created. Location header and body contain the new order ID.</response>
    /// <response code="400">The request body was missing or one or more product IDs were not found in the catalogue.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        var result = await _orderService.CreateOrderAsync(request, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { error = "One or more product IDs are invalid.", invalidProductIds = result.InvalidProductIds });
        }

        return CreatedAtAction(nameof(GetOrderById), new { orderId = result.OrderId }, new { id = result.OrderId });
    }

    /// <summary>
    /// Returns total profit grouped by year and month for all Completed orders.
    /// </summary>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>200 OK with a list of monthly profit aggregates ordered by year and month ascending.</returns>
    /// <response code="200">A list of monthly profit aggregates for all Completed orders.</response>
    [HttpGet("profit/monthly")]
    [ProducesResponseType(typeof(IEnumerable<MonthlyProfit>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlyProfit(CancellationToken cancellationToken)
    {
        var profit = await _orderService.GetMonthlyProfitAsync(cancellationToken);
        return Ok(profit);
    }
}
