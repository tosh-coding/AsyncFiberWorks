using System.Threading;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// </summary>
    public class ThreadFiber : ThreadFiberSlim, IFiber
    {
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Create a thread fiber with the default queue.
        /// </summary>
        public ThreadFiber() 
            : base()
        {}

        /// <summary>
        /// Creates a thread fiber with a specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadFiber(IQueue queue) 
            : base(queue)
        {}

        /// <summary>
        /// Creates a thread fiber with a specified name.
        /// </summary>
        /// /// <param name="threadName"></param>
        public ThreadFiber(string threadName)
            : base(threadName)
        {}

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ThreadFiber(IQueue queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
            : base(queue, threadName, isBackground, priority)
        {
        }

        /// <summary>
        /// Stops the thread.
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public override void Dispose()
        {
            _subscriptions?.Dispose();
            base.Dispose();
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
