using FluentValidation;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.CreateOrder;

/// <summary>
/// Validates a CreateOrderCommand before it is processed by the handler.
/// </summary>
public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    /// <summary>
    /// Represents the maximum number of items allowed in a single order to prevent abuse and ensure performance.
    /// </summary>
    private const int MaxOrderItems = 100;

    /// <summary>
    /// Represents the maximum allowed quantity for a single item.
    /// </summary>
    private const int MaxQuantityPerItem = 1_000_000;

    /// <summary>
    /// Defines validation rules: ResellerId and CustomerId must be non-empty GUIDs;
    /// Items must be a non-null, non-empty list (max 100) with no null entries and no duplicate ProductIds;
    /// each item needs a valid ProductId and a Quantity between 1 and 1,000,000.
    /// </summary>
    public CreateOrderValidator()
    {
        RuleFor(command => command.ResellerId).NotEmpty().WithMessage("ResellerId is required.");
        RuleFor(command => command.CustomerId).NotEmpty().WithMessage("CustomerId is required.");
        RuleFor(command => command.Items)
            .NotNull().WithMessage("Items must not be null.")
            .NotEmpty().WithMessage("Order must contain at least one item.");
        RuleFor(command => command.Items)
            .Must(items => items.Count() <= MaxOrderItems)
            .When(command => command.Items != null)
            .WithMessage($"An order cannot contain more than {MaxOrderItems} items.");
        RuleFor(command => command.Items)
            .Must(items => items.All(item => item != null))
            .When(command => command.Items != null)
            .WithMessage("Order items cannot contain null entries.");
        RuleFor(command => command.Items)
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count())
            .When(command => command.Items is { Count: > 1 } && command.Items.All(item => item != null))
            .WithMessage("Duplicate ProductIds are not allowed in a single order.");
        RuleForEach(command => command.Items).Where(item => item != null).ChildRules(item =>
        {
            item.RuleFor(orderItem => orderItem.ProductId).NotEmpty().WithMessage("ProductId is required.");
            item.RuleFor(orderItem => orderItem.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
                .LessThanOrEqualTo(MaxQuantityPerItem).WithMessage($"Quantity cannot exceed {MaxQuantityPerItem:N0}.");
        });
    }
}
