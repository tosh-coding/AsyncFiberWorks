namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Sequential executor of actions.
    /// It is part of the message queue for the message-passing mechanism.
    /// </summary>
    public interface IFiber : IExecutionContext, IAsyncExecutionContext
    {
    }
}
