using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace OrderApi.Tests.Common;

/// <summary>
/// Fake authentication handler that auto-authenticates every test request.
/// Replaces JWT Bearer validation so integration tests run without a real identity provider.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Initialises the handler with the required ASP.NET Core authentication infrastructure.
    /// </summary>
    /// <param name="options">The scheme options monitor provided by the authentication infrastructure.</param>
    /// <param name="logger">The logger factory used by the base handler.</param>
    /// <param name="encoder">The URL encoder used by the base handler.</param>
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    /// <summary>
    /// Returns an <see cref="AuthenticateResult"/> that is always successful,
    /// allowing integration tests to bypass real JWT Bearer validation.
    /// The resulting principal carries the minimum claims required by the API authorisation policies.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing a successful <see cref="AuthenticateResult"/>
    /// with a <see cref="ClaimsPrincipal"/> that holds read and write order scopes.
    /// </returns>
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
