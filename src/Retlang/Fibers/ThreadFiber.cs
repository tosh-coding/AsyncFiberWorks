using System.Threading;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// </summary>
    public class ThreadFiber : FiberWithDisposableList
    {
        private readonly ThreadFiberSlim _threadFiberSlim;

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
            : base(fiber)
        {
            _threadFiberSlim = fiber;
        }

        /// <summary>
        /// <see cref="IFiber"/>
        /// </summary>
        public Thread Thread
        {
            get { return _threadFiberSlim.Thread; }
        }

        /// <summary>
        /// <see cref="IFiber.Start()"/>
        /// </summary>
        public override void Start()
        {
            base.Start();
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
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}