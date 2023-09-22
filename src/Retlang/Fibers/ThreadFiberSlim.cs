using System;
using System.Threading;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// </summary>
    public class ThreadFiberSlim : IExecutionContext, IDisposable
    {
        private static int THREAD_COUNT;
        private readonly Thread _thread;
        private readonly IQueue _queue;

        /// <summary>
        /// Create a thread fiber with the default queue.
        /// </summary>
        public ThreadFiberSlim() 
            : this(new DefaultQueue())
        {}

        /// <summary>
        /// Creates a thread fiber with the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadFiberSlim(IQueue queue) 
            : this(queue, "ThreadFiber-" + GetNextThreadId())
        {}

        /// <summary>
        /// Creates a thread fiber with the specified name.
        /// </summary>
        /// /// <param name="threadName"></param>
        public ThreadFiberSlim(string threadName)
            : this(new DefaultQueue(), threadName)
        {}

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ThreadFiberSlim(IQueue queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            _queue = queue;
            _thread = new Thread(RunThread);
            _thread.Name = threadName;
            _thread.IsBackground = isBackground;
            _thread.Priority = priority;
        }

        /// <summary>
        /// Create a thread fiber with the default queue and call the Start method.
        /// </summary>
        /// <returns></returns>
        public static ThreadFiberSlim StartNew()
        {
            var fiber = new ThreadFiberSlim();
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Creates a thread fiber with a specified queue and call the Start method.
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public static ThreadFiberSlim StartNew(IQueue queue)
        {
            var fiber = new ThreadFiberSlim(queue);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Creates a thread fiber with a specified name and call the Start method.
        /// </summary>
        /// <param name="threadName"></param>
        /// <returns></returns>
        public static ThreadFiberSlim StartNew(string threadName)
        {
            var fiber = new ThreadFiberSlim(threadName);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Create a new instance and call the Start method.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static ThreadFiberSlim StartNew(IQueue queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            var fiber = new ThreadFiberSlim(queue, threadName, isBackground, priority);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// The dedicated thread.
        /// </summary>
        public Thread Thread
        {
            get { return _thread; }
        }

        private static int GetNextThreadId()
        {
            return Interlocked.Increment(ref THREAD_COUNT);
        }

        private void RunThread()
        {
            _queue.Run();
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Enqueue(action);
        }

        /// <summary>
        /// Start the thread.
        /// </summary>
        public void Start()
        {
            _thread.Start();
        }

        ///<summary>
        /// Calls join on the thread.
        ///</summary>
        public void Join()
        {
            _thread.Join();
        }

        /// <summary>
        /// Stops the thread.
        /// </summary>
        public void Dispose()
        {
            _queue.Stop();
        }
    }
}