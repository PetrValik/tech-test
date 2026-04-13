using NUnit.Framework;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Order.API.Tests.Helpers;

/// <summary>
/// Abstract base class shared by all Orders API integration-test fixtures.
/// Spins up the full ASP.NET Core pipeline via <see cref="OrderApiFactory"/> and
/// exposes a pre-seeded <see cref="SeedData"/> instance before each test.
/// </summary>
[TestFixture]
public abstract class ApiTestBase
{
    /// <summary>
    /// The WebApplicationFactory that hosts the test server.
    /// </summary>
    protected OrderApiFactory _factory = null!;

    /// <summary>
    /// HTTP client wired to the in-process test server.
    /// </summary>
    protected HttpClient _client = null!;

    /// <summary>
    /// Reference-data identifiers seeded before each test.
    /// </summary>
    protected SeedData _seed = null!;

    /// <summary>
    /// JSON deserialisation options shared by all tests (case-insensitive).
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a fresh factory, client, and seeded database before each test.
    /// </summary>
    [SetUp]
    public async Task SetUp()
    {
        _factory = new OrderApiFactory();
        _client  = _factory.CreateClient();
        _seed    = await _factory.ResetDatabase();
    }

    /// <summary>
    /// Disposes the HTTP client and factory after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// Reads the response body and deserialises it as <typeparamref name="T"/>.
    /// </summary>
    protected static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
    }
}
