using MediatR;
using FluentValidation;
using NetArchTest.Rules;
using System.Reflection;

namespace OrderApi.Tests;

/// <summary>
/// Architecture tests that enforce structural rules across the codebase.
/// These tests prevent accidental coupling between feature slices and ensure
/// consistent patterns are followed throughout the project.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    /// <summary>
    /// Verifies that types in the OrderApi.Features namespace do not reference
    /// anything in the OrderApi.Middleware namespace.
    /// </summary>
    [Fact]
    public void Features_ShouldNotDependOnMiddleware()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("OrderApi.Features")
            .ShouldNot().HaveDependencyOn("OrderApi.Middleware")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// Verifies that types in the OrderApi.Middleware namespace do not reference
    /// anything in the OrderApi.Features namespace.
    /// </summary>
    [Fact]
    public void Middleware_ShouldNotDependOnFeatures()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("OrderApi.Middleware")
            .ShouldNot().HaveDependencyOn("OrderApi.Features")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// Verifies that all classes whose names end with "Handler" implement <see cref="IRequestHandler{TRequest, TResponse}"/>.
    /// </summary>
    [Fact]
    public void AllHandlers_ShouldImplementIRequestHandler()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Handler")
            .And().AreClasses()
            .And().AreNotAbstract()
            .And().DoNotResideInNamespace("OrderApi.Exceptions")
            .Should().ImplementInterface(typeof(IRequestHandler<,>))
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// Verifies that all classes whose names end with "Validator" inherit from
    /// <see cref="AbstractValidator{T}"/>.
    /// </summary>
    [Fact]
    public void AllValidators_ShouldInheritAbstractValidator()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Validator")
            .And().AreClasses()
            .Should().Inherit(typeof(AbstractValidator<>))
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// Verifies that all non-abstract handler classes are declared as sealed
    /// to prevent unintended inheritance.
    /// </summary>
    [Fact]
    public void Handlers_ShouldBeSealed()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Handler")
            .And().AreClasses()
            .And().AreNotAbstract()
            .Should().BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// Verifies that types in the OrderApi.Common namespace do not reference
    /// anything in the OrderApi.Features namespace.
    /// </summary>
    [Fact]
    public void CommonBehaviors_ShouldNotDependOnSpecificFeatures()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("OrderApi.Common")
            .ShouldNot().HaveDependencyOn("OrderApi.Features")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// Verifies that types in the OrderApi.Infrastructure namespace do not reference
    /// anything in the OrderApi.Features namespace.
    /// </summary>
    [Fact]
    public void Infrastructure_ShouldNotDependOnFeatures()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("OrderApi.Infrastructure")
            .ShouldNot().HaveDependencyOn("OrderApi.Features")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// Formats a failure message for an architecture test result, listing the types that violated the rule.
    /// </summary>
    /// <param name="result">The <see cref="TestResult"/> containing the details of the architecture test.</param>
    /// <returns>A formatted string describing the architecture violation and the names of the failing types.</returns>
    private static string FailureMessage(TestResult result) =>
        $"Architecture rule violated by: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
