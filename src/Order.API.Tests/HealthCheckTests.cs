using NUnit.Framework;
using Order.API.Tests.Helpers;
using System.Net;
using System.Threading.Tasks;

namespace Order.API.Tests;

/// <summary>
/// Integration tests for the health-check endpoints (GET /health/ready, GET /health/live).
/// </summary>
[TestFixture]
public class HealthCheckTests : ApiTestBase
{
    /// <summary>
    /// GET /health/ready returns 200 OK with "Healthy" body.
    /// </summary>
    [Test]
    public async Task Health_Returns200_Healthy()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Healthy"));
    }
}
