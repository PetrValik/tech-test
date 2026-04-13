using Microsoft.Extensions.DependencyInjection;
using OrderApi.Infrastructure;
using OrderApi.Tests.Common;
using System.Net;
using System.Net.Http.Headers;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for correlation ID and response compression middleware.
/// Covers X-Correlation-ID propagation and gzip Content-Encoding on large responses.
/// </summary>
[Collection("Orders")]
public sealed class MiddlewareTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public MiddlewareTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that every response includes an X-Correlation-ID header containing a valid GUID.
    /// </summary>
    [Fact]
    public async Task Response_Contains_CorrelationId_Header()
    {
        var response = await Client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var values = response.Headers.GetValues("X-Correlation-ID").ToList();
        Assert.Single(values);
        Assert.True(Guid.TryParse(values[0], out _));
    }

    /// <summary>
    /// Verifies that when a client sends an X-Correlation-ID header, the same value is echoed back.
    /// </summary>
    [Fact]
    public async Task Response_Echoes_Client_CorrelationId()
    {
        var clientId = Guid.NewGuid().ToString("D");
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready");
        request.Headers.Add("X-Correlation-ID", clientId);

        var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var returnedId = response.Headers.GetValues("X-Correlation-ID").Single();
        Assert.Equal(clientId, returnedId);
    }

    /// <summary>
    /// Verifies that responses are gzip-compressed when the client sends Accept-Encoding: gzip.
    /// Uses a raw handler to prevent the test client from transparently decompressing the response.
    /// </summary>
    [Fact]
    public async Task Response_Is_Compressed_When_AcceptEncoding_Gzip()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orders");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        var handler = Factory.Server.CreateHandler();
        using var rawClient = new HttpClient(handler) { BaseAddress = Client.BaseAddress };

        var response = await rawClient.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("gzip", response.Content.Headers.ContentEncoding.FirstOrDefault());
    }
}
