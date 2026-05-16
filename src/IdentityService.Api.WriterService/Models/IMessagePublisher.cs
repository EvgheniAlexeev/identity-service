// FILE: IMessagePublisher.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WRITER
// PURPOSE: M-IDENTITY-WRITER component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WRITER

/// <summary>
/// Interface for publishing commands to the message bus (Wolverine-compatible).
/// Used as abstraction to enable test mocking.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publish a command to the message bus.
    /// </summary>
    Task PublishAsync<T>(T command, CancellationToken ct = default) where T : class;
}
