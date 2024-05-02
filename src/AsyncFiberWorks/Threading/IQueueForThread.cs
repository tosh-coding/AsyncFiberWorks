using System;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Holds on to actions until the execution context can process them.
    /// </summary>
    public interface IQueueForThread : IThreadWork, IQueuingContextForThread
    {
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
