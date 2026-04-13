using FluentValidation;
using Order.Model;
using System.Linq;

namespace Order.WebAPI.Validators;

/// <summary>
/// FluentValidation validator for CreateOrderRequest.
/// </summary>
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    /// <summary>
    /// Defines maximum limits for order items and quantity to prevent excessively large orders that could impact system performance.
    /// </summary>
    private const int MaxOrderItems = 100;

    /// <summary>
    /// Represents the maximum allowed quantity for a single item.
    /// </summary>
    private const int MaxQuantityPerItem = 1_000_000;

    /// <summary>
    /// Defines validation rules: ResellerId and CustomerId must be non-empty,
    /// Items must be a non-null, non-empty list (max 100) with no null entries and no duplicate ProductIds,
    /// each item needs a valid ProductId and a Quantity between 1 and 1,000,000.
    /// </summary>
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.ResellerId).NotEmpty().WithMessage("ResellerId is required.");
        RuleFor(request => request.CustomerId).NotEmpty().WithMessage("CustomerId is required.");
        RuleFor(request => request.Items)
            .NotNull().WithMessage("Items must not be null.")
            .NotEmpty().WithMessage("Order must contain at least one item.");
        RuleFor(request => request.Items)
            .Must(items => items.Count <= MaxOrderItems)
            .When(request => request.Items != null)
            .WithMessage($"An order cannot contain more than {MaxOrderItems} items.");
        RuleFor(request => request.Items)
            .Must(items => items.All(item => item != null))
            .When(request => request.Items != null)
            .WithMessage("Order items cannot contain null entries.");
        RuleFor(request => request.Items)
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count)
            .When(request => request.Items is { Count: > 1 } && request.Items.All(item => item != null))
            .WithMessage("Duplicate ProductIds are not allowed in a single order.");
        RuleForEach(request => request.Items).Where(item => item != null).ChildRules(item =>
        {
            item.RuleFor(lineItem => lineItem.ProductId).NotEmpty().WithMessage("ProductId is required.");
            item.RuleFor(lineItem => lineItem.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
                .LessThanOrEqualTo(MaxQuantityPerItem).WithMessage($"Quantity cannot exceed {MaxQuantityPerItem:N0}.");
        });
    }
}
