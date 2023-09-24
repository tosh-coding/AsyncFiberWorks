using System;
using System.Threading;
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
            : this(new ThreadFiberSlim())
        {}

        /// <summary>
        /// Creates a thread fiber with a specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadFiber(IQueue queue) 
            : this(new ThreadFiberSlim(queue))
        {}

        /// <summary>
        /// Creates a thread fiber with a specified name.
        /// </summary>
        /// /// <param name="threadName"></param>
        public ThreadFiber(string threadName)
            : this(new ThreadFiberSlim(threadName))
        {}

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ThreadFiber(IQueue queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
            : this(new ThreadFiberSlim(queue, threadName, isBackground, priority))
        {
        }

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        private ThreadFiber(ThreadFiberSlim fiber)
        {
            _threadFiberSlim = fiber;
        }

        /// <summary>
        /// Create a thread fiber with the default queue, and call the Start method.
        /// </summary>
        /// <returns></returns>
        public static ThreadFiber StartNew()
        {
            var fiber = new ThreadFiber();
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Creates a thread fiber with a specified queue, and call the Start method.
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public static ThreadFiber StartNew(IQueue queue)
        {
            var fiber = new ThreadFiber(queue);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Creates a thread fiber with a specified name, and call the Start method.
        /// </summary>
        /// <param name="threadName"></param>
        /// <returns></returns>
        public static ThreadFiber StartNew(string threadName)
        {
            var fiber = new ThreadFiber(threadName);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Creates a thread fiber and call the Start method.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static ThreadFiber StartNew(IQueue queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            var fiber = new ThreadFiber(queue, threadName, isBackground, priority);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// The thread.
        /// </summary>
        public Thread Thread
        {
            get { return _threadFiberSlim.Thread; }
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
        /// <see cref="IExecutionContext.Enqueue(Action)"/>
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _threadFiberSlim.Enqueue(action);
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistryGetter.FallbackDisposer"/>
        /// </summary>
        public ISubscriptionRegistry FallbackDisposer
        {
            get { return _subscriptions; }
        }
    }
}