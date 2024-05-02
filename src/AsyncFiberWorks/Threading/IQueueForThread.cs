using System;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Holds on to actions until the execution context can process them.
    /// </summary>
    public interface IQueueForThread : IConsumerQueueForThread, IQueuingContextForThread
    {
    }

    /// <summary>
    /// Holds on to actions until the execution context can process them.
    /// </summary>
    public interface IConsumerQueueForThread
    {
        /// <summary>
        /// Start consuming actions.
        /// Does not return from the call until it stops.
        /// </summary>
        void Run();

        /// <summary>
        /// Stop consuming actions.
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Queuing interface of a worker thread.
    /// </summary>
    public interface IQueuingContextForThread
    {
        ///<summary>
        /// Enqueues action for execution context to process.
        ///</summary>
        ///<param name="action"></param>
        void Enqueue(Action action);
    }
}
