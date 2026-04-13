using Microsoft.AspNetCore.OutputCaching;

namespace OrderApi.Tests.Common;

/// <summary>
/// A no-op cache store that never stores or returns cached responses.
/// Used in tests to disable output caching so stale cached data cannot leak between tests.
/// </summary>
internal sealed class NoOpOutputCacheStore : IOutputCacheStore
{
    /// <inheritdoc />
    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken) => ValueTask.FromResult<byte[]?>(null);

    /// <inheritdoc />
    public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
