using NUnit.Framework;
using Order.API.Tests.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Order.API.Tests;

/// <summary>
/// Integration tests for cross-cutting middleware behaviour (correlation ID propagation).
/// </summary>
[TestFixture]
public class MiddlewareTests : ApiTestBase
{
    /// <summary>
    /// Every response includes an X-Correlation-ID header (auto-generated when not supplied).
    /// </summary>
    [Test]
    public async Task Response_Contains_CorrelationId_Header()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.Contains("X-Correlation-ID"), Is.True);

        var values = response.Headers.GetValues("X-Correlation-ID").ToList();
        Assert.That(values.Count, Is.EqualTo(1));
        Assert.That(Guid.TryParse(values[0], out _), Is.True);
    }

    /// <summary>
    /// When the client sends an X-Correlation-ID header, the same value is returned.
    /// </summary>
    [Test]
    public async Task Response_Echoes_Client_CorrelationId()
    {
        var clientId = Guid.NewGuid().ToString("D");
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready");
        request.Headers.Add("X-Correlation-ID", clientId);

        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var returnedId = response.Headers.GetValues("X-Correlation-ID").Single();
        Assert.That(returnedId, Is.EqualTo(clientId));
    }
}
