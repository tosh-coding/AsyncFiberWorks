using System;
using System.Threading;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// This fiber consumes tasks via a single-stage queue.
    /// </summary>
    public class ThreadFiber : IExecutionContext, IDisposable
    {
        private readonly IDedicatedConsumerThreadWork _queue;
        private readonly UserWorkerThread _workerThread;

        /// <summary>
        /// Create a thread fiber with the default queue.
        /// </summary>
        public ThreadFiber()
            : this(new DefaultQueue())
        {
        }

        /// <summary>
        /// Create a thread fiber with the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadFiber(IDedicatedConsumerThreadWork queue)
            : this(queue, null)
        {
        }

        /// <summary>
        /// Create a thread fiber with the specified thread name.
        /// </summary>
        /// <param name="threadName"></param>
        public ThreadFiber(string threadName)
            : this(new DefaultQueue(), threadName)
        {
        }

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ThreadFiber(IDedicatedConsumerThreadWork queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            _queue = queue;
            _workerThread = new UserWorkerThread(queue.Run, threadName, isBackground, priority);
            _workerThread.Start();
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Stop()
        {
            _queue.Stop();
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Enqueue(action);
        }
    }
}
