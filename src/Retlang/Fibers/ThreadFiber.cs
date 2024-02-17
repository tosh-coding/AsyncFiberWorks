using System;
using System.Threading;
using System.Threading.Tasks;
using Retlang.Channels;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// </summary>
    public class ThreadFiber : IFiber
    {
        private readonly IConsumerThread _workerThread;
        private readonly Subscriptions _subscriptions = new Subscriptions();
        private bool _stopped = false;

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
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="consumerThread">A consumer thread.</param>
        public ThreadFiber(IConsumerThread consumerThread)
        {
            _workerThread = consumerThread;
        }

        /// <summary>
        /// Start the thread.
        /// </summary>
        public void Start()
        {
            _workerThread.Start();
        }

        /// <summary>
        /// Clear all subscriptions and schedules. Then stop threads.
        /// </summary>
        public void Stop()
        {
            if (!_stopped)
            {
                _subscriptions.Dispose();
                _workerThread.Stop();
                _stopped = true;
            }
        }

        ///<summary>
        /// Calls join on the thread.
        ///</summary>
        public Task Join()
        {
            return _workerThread.Join();
        }

        /// <summary>
        /// Clear all subscriptions and schedules. Then stop threads.
        /// </summary>
        public void Dispose()
        {
            Stop();
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
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _workerThread.Enqueue(action);
        }
    }
}
