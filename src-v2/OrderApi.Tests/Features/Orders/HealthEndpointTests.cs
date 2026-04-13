using OrderApi.Tests.Common;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for health check endpoints — liveness and readiness probes.
/// </summary>
[Collection("Orders")]
public sealed class HealthEndpointTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public HealthEndpointTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that GET /health/ready returns 200 OK with a body indicating the service is healthy.
    /// </summary>
    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health/ready");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body);
    }
}
