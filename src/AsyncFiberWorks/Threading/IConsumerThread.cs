using System;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// An interface for adding actions to a worker thread.
    /// </summary>
    public interface IConsumerThread
    {
        /// <summary>
        /// Enqueue an action to a worker thread.
        /// </summary>
        /// <param name="action"></param>
        void Enqueue(Action action);
    }
}
