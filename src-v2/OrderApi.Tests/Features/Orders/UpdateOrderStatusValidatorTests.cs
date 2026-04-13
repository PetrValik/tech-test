using FluentValidation.TestHelper;
using OrderApi.Features.Orders.UpdateOrderStatus;
using OrderApi.Infrastructure;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Unit tests for <see cref="UpdateOrderStatusValidator"/> — validates order status update commands.
/// No database or HTTP calls needed.
/// </summary>
public class UpdateOrderStatusValidatorTests
{
    private readonly UpdateOrderStatusValidator _validator = new();

    /// <summary>
    /// Verifies that validation fails when <see cref="UpdateOrderStatusCommand.StatusName"/> is an empty string.
    /// </summary>
    [Fact]
    public void UpdateStatus_Fails_WhenStatusNameEmpty()
    {
        _validator.TestValidate(new UpdateOrderStatusCommand(Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(x => x.StatusName);
    }

    /// <summary>
    /// Verifies that validation fails when <see cref="UpdateOrderStatusCommand.StatusName"/> is not one
    /// of the recognised values in <see cref="OrderStatusNames.All"/>.
    /// </summary>
    [Fact]
    public void UpdateStatus_Fails_WhenStatusInvalid()
    {
        _validator.TestValidate(new UpdateOrderStatusCommand(Guid.NewGuid(), "NotAStatus"))
            .ShouldHaveValidationErrorFor(x => x.StatusName);
    }

    /// <summary>
    /// Verifies that every status name defined in <see cref="OrderStatusNames.All"/>
    /// passes validation without errors.
    /// </summary>
    [Fact]
    public void UpdateStatus_Passes_WithAllValidStatuses()
    {
        foreach (var status in OrderStatusNames.All)
        {
            _validator.TestValidate(new UpdateOrderStatusCommand(Guid.NewGuid(), status))
                .ShouldNotHaveAnyValidationErrors();
        }
    }

    /// <summary>
    /// Verifies that validation is case-insensitive — lowercase and uppercase status name variants
    /// must both pass without errors.
    /// </summary>
    [Fact]
    public void UpdateStatus_Passes_WithLowercaseStatusName()
    {
        // Validator is case-insensitive; "completed" and "CREATED" must both be accepted.
        _validator.TestValidate(new UpdateOrderStatusCommand(Guid.NewGuid(), "completed"))
            .ShouldNotHaveAnyValidationErrors();
        _validator.TestValidate(new UpdateOrderStatusCommand(Guid.NewGuid(), "CREATED"))
            .ShouldNotHaveAnyValidationErrors();
    }
}
