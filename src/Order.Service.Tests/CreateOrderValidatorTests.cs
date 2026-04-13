using FluentValidation;
using NUnit.Framework;
using Order.Model;
using Order.WebAPI.Validators;
using System;
using System.Linq;

namespace Order.Service.Tests;

/// <summary>
/// Unit tests for <see cref="CreateOrderRequestValidator"/>.
/// Covers required-field validation, item quantity limits, duplicate product IDs, and null elements.
/// </summary>
[TestFixture]
public class CreateOrderValidatorTests
{
    private CreateOrderRequestValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new CreateOrderRequestValidator();
    }

    [Test]
    public void CreateOrder_Fails_WhenResellerIdEmpty()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.Empty,
            CustomerId = Guid.NewGuid(),
            Items = new[] { new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 } }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateOrderRequest.ResellerId)), Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenCustomerIdEmpty()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.Empty,
            Items = new[] { new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 } }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateOrderRequest.CustomerId)), Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenItemsEmpty()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = Array.Empty<CreateOrderItemRequest>()
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateOrderRequest.Items)), Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenItemQuantityIsZero()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[] { new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 0 } }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName.Contains("Quantity")), Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenItemProductIdEmpty()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[] { new CreateOrderItemRequest { ProductId = Guid.Empty, Quantity = 1 } }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName.Contains("ProductId")), Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenItemsIsNull()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = null!
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateOrderRequest.Items)), Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenItemQuantityIsNegative()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[] { new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = -5 } }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName.Contains("Quantity")), Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenQuantityExceedsMax()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[] { new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1_000_001 } }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName.Contains("Quantity")), Is.True);
    }

    [Test]
    public void CreateOrder_Passes_WhenQuantityIsMaximum()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[] { new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1_000_000 } }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenDuplicateProductIds()
    {
        var productId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new CreateOrderItemRequest { ProductId = productId, Quantity = 1 },
                new CreateOrderItemRequest { ProductId = productId, Quantity = 2 }
            }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateOrderRequest.Items)), Is.True);
    }

    [Test]
    public void CreateOrder_Fails_WhenItemsContainNullElement()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new CreateOrderItemRequest[] { new() { ProductId = Guid.NewGuid(), Quantity = 1 }, null! }
        };
        // Must not throw NullReferenceException; null element captured by RuleForEach.
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void CreateOrder_Passes_WithValidRequest()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[] { new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 5 } }
        };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.True);
    }
}
