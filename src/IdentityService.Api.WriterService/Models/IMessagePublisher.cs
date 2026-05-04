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
