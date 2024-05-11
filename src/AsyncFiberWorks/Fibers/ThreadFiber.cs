using System;
using System.Threading;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// </summary>
    public class ThreadFiber : IFiber, IDisposable, IExecutionContext
    {
        private readonly IDedicatedConsumerThreadWork _queue;
        private readonly UserWorkerThread _workerThread;
        private bool _stopped = false;

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
            _workerThread = new UserWorkerThread(queue, threadName, isBackground, priority);
            _workerThread.Start();
        }

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="consumerThread">A consumer thread.</param>
        public ThreadFiber(UserWorkerThread consumerThread)
        {
            _workerThread = consumerThread;
            _workerThread.Start();
        }

        /// <summary>
        /// Clear all subscriptions and schedules. Then stop threads.
        /// </summary>
        public void Stop()
        {
            if (!_stopped)
            {
                _workerThread.Stop();
                _stopped = true;
            }
        }

        /// <summary>
        /// Clear all subscriptions and schedules. Then stop threads.
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
