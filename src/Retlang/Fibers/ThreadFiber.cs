using System;
using System.Threading;
using Retlang.Channels;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// </summary>
    public class ThreadFiber : IFiber
    {
        private readonly UserWorkerThread _workerThread;
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Create a thread fiber with the default queue.
        /// </summary>
        public ThreadFiber()
        {
            _workerThread = new UserWorkerThread();
        }

        /// <summary>
        /// Create a thread fiber with the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadFiber(IQueueForThread queue)
        {
            _workerThread = new UserWorkerThread(queue);
        }

        /// <summary>
        /// Create a thread fiber with the specified thread name.
        /// </summary>
        /// <param name="threadName"></param>
        public ThreadFiber(string threadName)
        {
            _workerThread = new UserWorkerThread(threadName);
        }

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ThreadFiber(IQueueForThread queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            _workerThread = new UserWorkerThread(queue, threadName, isBackground, priority);
        }

        /// <summary>
        /// Start the thread.
        /// </summary>
        public void Start()
        {
            _workerThread.Start();
        }

        ///<summary>
        /// Calls join on the thread.
        ///</summary>
        public void Join()
        {
            _workerThread.Join();
        }

        /// <summary>
        /// Clear all subscriptions and schedules. Then stop threads.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
            _workerThread.Dispose();
        }


        /// <summary>
        /// <see cref="ISubscriptionRegistry.CreateSubscription"/>
        /// </summary>
        public Unsubscriber CreateSubscription()
        {
            return _subscriptions.CreateSubscription();
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistry.NumSubscriptions"/>
        /// </summary>
        public int NumSubscriptions
        {
            get { return _subscriptions.NumSubscriptions; }
        }

        /// <summary>
        /// The dedicated thread.
        /// </summary>
        public Thread Thread
        {
            get { return _workerThread.Thread; }
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _workerThread.Queue((x) => action());
        }
    }
}
