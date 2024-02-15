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
        private readonly ThreadFiberSlim _threadFiberSlim;
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Create a thread fiber with the default queue.
        /// </summary>
        public ThreadFiber()
        {
            _threadFiberSlim = new ThreadFiberSlim();
        }

        /// <summary>
        /// Creates a thread fiber with a specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadFiber(IQueue queue)
        {
            _threadFiberSlim = new ThreadFiberSlim(queue);
        }

        /// <summary>
        /// Creates a thread fiber with a specified name.
        /// </summary>
        /// /// <param name="threadName"></param>
        public ThreadFiber(string threadName)
        {
            _threadFiberSlim = new ThreadFiberSlim(threadName);
        }

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ThreadFiber(IQueue queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            _threadFiberSlim = new ThreadFiberSlim(queue, threadName, isBackground, priority);
        }

        /// <summary>
        /// Start the thread.
        /// </summary>
        public void Start()
        {
            _threadFiberSlim.Start();
        }

        ///<summary>
        /// Calls join on the thread.
        ///</summary>
        public void Join()
        {
            _threadFiberSlim.Join();
        }

        /// <summary>
        /// Stops the thread.
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
            _threadFiberSlim.Dispose();
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
            get { return _threadFiberSlim.Thread; }
        }

        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _threadFiberSlim.Enqueue(action);
        }
    }
}
