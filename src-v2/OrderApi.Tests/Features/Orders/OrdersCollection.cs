using OrderApi.Tests.Common;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Declares the xUnit collection that shares a single <see cref="OrderApiTestFactory"/> instance
/// across all order endpoint test classes. Tests in this collection run sequentially to prevent
/// concurrent WebApplicationFactory initialisation failures.
/// </summary>
[CollectionDefinition("Orders")]
public sealed class OrdersCollection : ICollectionFixture<OrderApiTestFactory>;
