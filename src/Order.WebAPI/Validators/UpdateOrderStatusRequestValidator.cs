using FluentValidation;
using Order.Model;
using System.Linq;

namespace Order.WebAPI.Validators;

/// <summary>
/// FluentValidation validator for UpdateOrderStatusRequest.
/// Rejects any status name not in the known whitelist to avoid a redundant DB round-trip.
/// </summary>
public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    /// <summary>
    /// Defines validation rules: StatusName must be non-empty and one of the known status values.
    /// </summary>
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(request => request.StatusName)
            .NotEmpty().WithMessage("StatusName is required.")
            .Must(statusName => OrderStatusNames.All.Contains(statusName))
            .WithMessage($"StatusName must be one of: {string.Join(", ", OrderStatusNames.All)}");
    }
}
