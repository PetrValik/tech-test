using FluentValidation;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.SearchOrders;

/// <summary>
/// Validates a SearchOrdersQuery before it reaches the handler.
/// The Status field is optional, but if provided it must be a known lifecycle value.
/// If both From and To are supplied, From must not be later than To.
/// </summary>
public class SearchOrdersValidator : AbstractValidator<SearchOrdersQuery>
{
    /// <summary>
    /// Defines validation rules: if Status is supplied it must be one of the four known values;
    /// if both From and To are supplied, From must not be after To.
    /// </summary>
    public SearchOrdersValidator()
    {
        When(query => !string.IsNullOrEmpty(query.Status), () =>
        {
            RuleFor(query => query.Status)
                .Must(statusValue => OrderStatusNames.All.Any(validStatus => string.Equals(validStatus, statusValue, StringComparison.OrdinalIgnoreCase)))
                .WithMessage($"Status must be one of: {string.Join(", ", OrderStatusNames.All)}");
        });

        When(query => query.From.HasValue && query.To.HasValue, () =>
        {
            RuleFor(query => query.From)
                .Must((query, from) => from!.Value <= query.To!.Value)
                .WithMessage("From must not be later than To.");
        });
    }
}
