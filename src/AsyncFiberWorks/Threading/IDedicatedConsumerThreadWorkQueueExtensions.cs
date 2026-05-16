using System;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Extension methods for IDedicatedConsumerThreadWorkQueue.
    /// </summary>
    public static class IDedicatedConsumerThreadWorkQueueExtensions
    {
        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="queue">Target queue.</param>
        /// <param name="action">Action to be executed.</param>
        public static void Enqueue(this IDedicatedConsumerThreadWorkQueue queue, Action action)
        {
            queue.Enqueue(ExecuteAction, action);
        }

        static void ExecuteAction(object state)
        {
            ((Action)state)?.Invoke();
        }
    }
}
