using System;

namespace Order.API.Tests.Helpers;

/// <summary>
/// Holds the byte-array identifiers seeded into the test database by
/// <see cref="OrderApiFactory.ResetDatabase"/>.
/// </summary>
public class SeedData
{
    /// <summary>
    /// Byte-array identifier for the Created order status.
    /// </summary>
    public byte[] StatusCreatedId    { get; } = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the Completed order status.
    /// </summary>
    public byte[] StatusCompletedId  { get; } = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the In Progress order status.
    /// </summary>
    public byte[] StatusInProgressId { get; } = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the Failed order status.
    /// </summary>
    public byte[] StatusFailedId     { get; } = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the Email order service.
    /// </summary>
    public byte[] ServiceEmailId     { get; } = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the 100GB Mailbox order product.
    /// </summary>
    public byte[] ProductEmailId     { get; } = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the Antivirus order service.
    /// </summary>
    public byte[] ServiceAntivirusId { get; } = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the Premium Antivirus order product.
    /// </summary>
    public byte[] ProductAntivirusId { get; } = Guid.NewGuid().ToByteArray();
}
