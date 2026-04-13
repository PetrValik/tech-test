using NUnit.Framework;
using Order.Model;
using Order.WebAPI.Validators;

namespace Order.Service.Tests;

/// <summary>
/// Unit tests for <see cref="UpdateOrderStatusRequestValidator"/>.
/// Covers empty status name, unrecognised names, and all valid status values.
/// </summary>
[TestFixture]
public class UpdateOrderStatusValidatorTests
{
    /// <summary>
    /// The validator instance under test, initialized in Setup().
    /// </summary>
    private UpdateOrderStatusRequestValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new UpdateOrderStatusRequestValidator();
    }

    [Test]
    public void UpdateStatus_Fails_WhenStatusNameEmpty()
    {
        var request = new UpdateOrderStatusRequest { StatusName = "" };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void UpdateStatus_Fails_WhenStatusNameInvalid()
    {
        var request = new UpdateOrderStatusRequest { StatusName = "NotAValidStatus" };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.False);
    }

    [TestCase("Created")]
    [TestCase("In Progress")]
    [TestCase("Failed")]
    [TestCase("Completed")]
    public void UpdateStatus_Passes_WithValidStatuses(string statusName)
    {
        var request = new UpdateOrderStatusRequest { StatusName = statusName };
        var result = _validator.Validate(request);
        Assert.That(result.IsValid, Is.True);
    }
}
