namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// The action driver provides the timing of execution.
    /// Provides methods for invoking and subscribing to actions.
    /// </summary>
    public interface IActionDriver : IActionDriverInvoker, IActionDriverSubscriber
    {
    }
}
