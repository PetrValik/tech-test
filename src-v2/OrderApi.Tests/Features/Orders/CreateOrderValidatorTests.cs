using FluentValidation.TestHelper;
using OrderApi.Features.Orders.CreateOrder;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Unit tests for <see cref="CreateOrderValidator"/> — validates order creation commands.
/// No database or HTTP calls needed.
/// </summary>
public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    /// <summary>
    /// Verifies that validation fails when <see cref="CreateOrderCommand.ResellerId"/> is <see cref="Guid.Empty"/>.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenResellerIdEmpty()
    {
        var command = new CreateOrderCommand(Guid.Empty, Guid.NewGuid(), [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ResellerId);
    }

    /// <summary>
    /// Verifies that validation fails when <see cref="CreateOrderCommand.CustomerId"/> is <see cref="Guid.Empty"/>.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenCustomerIdEmpty()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.Empty, [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    /// <summary>
    /// Verifies that validation fails when <see cref="CreateOrderCommand.Items"/> is an empty collection.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenItemsEmpty()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<CreateOrderItemRequest>());
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Items);
    }

    /// <summary>
    /// Verifies that validation fails when the item's <see cref="CreateOrderItemRequest.Quantity"/> is zero.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenQuantityZero()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), [new CreateOrderItemRequest(Guid.NewGuid(), 0)]);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    /// <summary>
    /// Verifies that validation fails when the item's <see cref="CreateOrderItemRequest.Quantity"/> is negative.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenQuantityNegative()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), [new CreateOrderItemRequest(Guid.NewGuid(), -5)]);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    /// <summary>
    /// Verifies that validation fails when the item's <see cref="CreateOrderItemRequest.ProductId"/> is <see cref="Guid.Empty"/>.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenItemProductIdEmpty()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), [new CreateOrderItemRequest(Guid.Empty, 1)]);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor("Items[0].ProductId");
    }

    /// <summary>
    /// Verifies that validation fails when <see cref="CreateOrderCommand.Items"/> is null.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenItemsNull()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), null!);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Items);
    }

    /// <summary>
    /// Verifies that validation fails when <see cref="CreateOrderCommand.Items"/> contains duplicate
    /// <see cref="CreateOrderItemRequest.ProductId"/> values.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenDuplicateProductIds()
    {
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(),
            [new CreateOrderItemRequest(productId, 1), new CreateOrderItemRequest(productId, 2)]);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Items);
    }

    /// <summary>
    /// Verifies that validation fails when the item's <see cref="CreateOrderItemRequest.Quantity"/> exceeds
    /// the maximum allowed value of 1 000 000.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenQuantityTooLarge()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(),
            [new CreateOrderItemRequest(Guid.NewGuid(), 1_000_001)]);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    /// <summary>
    /// Verifies that validation fails when <see cref="CreateOrderCommand.Items"/> contains a null element.
    /// The collection-level Must(all non-null) rule must catch this and produce a validation error
    /// rather than throwing a <see cref="NullReferenceException"/>.
    /// </summary>
    [Fact]
    public void CreateOrder_Fails_WhenItemsContainNullElement()
    {
        // RuleForEach with ChildRules silently skips null elements; the collection-level
        // Must(all non-null) rule must catch this and return 400 instead of throwing.
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(),
            [new CreateOrderItemRequest(Guid.NewGuid(), 1), null!]);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Items);
    }

    /// <summary>
    /// Verifies that a well-formed <see cref="CreateOrderCommand"/> with valid IDs, one item,
    /// and a valid quantity passes validation without any errors.
    /// </summary>
    [Fact]
    public void CreateOrder_Passes_WithValidRequest()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), [new CreateOrderItemRequest(Guid.NewGuid(), 5)]);
        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
