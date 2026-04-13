using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Order.API.Tests.Helpers;

/// <summary>
/// Fake authentication handler that auto-authenticates every test request.
/// Replaces JWT Bearer validation so integration tests run without a real IDP.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Initialises the handler with the required ASP.NET Core authentication infrastructure.
    /// </summary>
    /// <param name="options">Authentication scheme options monitor.</param>
    /// <param name="logger">Logger factory for the base handler.</param>
    /// <param name="encoder">URL encoder used by the base handler.</param>
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    /// <summary>
    /// Returns a successful authentication ticket containing fixed test claims
    /// for name, user ID, and order read/write scopes.
    /// </summary>
    /// <returns>A completed task with a successful <see cref="AuthenticateResult"/>.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("scope", "orders:read"),
            new Claim("scope", "orders:write")
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
