using FluentValidation;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.UpdateOrderStatus;

/// <summary>
/// Validates an UpdateOrderStatusCommand before it reaches the handler.
/// Rejects unknown status names at the validation layer to avoid an unnecessary database round-trip.
/// </summary>
public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    /// <summary>
    /// Defines validation rules: StatusName must be non-empty and one of the four known lifecycle values.
    /// </summary>
    public UpdateOrderStatusValidator()
    {
        RuleFor(command => command.StatusName)
            .NotEmpty().WithMessage("StatusName is required.")
            .Must(statusName => OrderStatusNames.All.Any(validStatus => string.Equals(validStatus, statusName, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"StatusName must be one of: {string.Join(", ", OrderStatusNames.All)}");
    }
}
