using System;
using System.Threading;

namespace Retlang.Core
{
    /// <summary>
    /// A thread pool implementation with only one worker thread.
    /// </summary>
    public sealed class UserWorkerThread : IThreadPool, IDisposable
    {
        private static int THREAD_COUNT;
        private readonly Thread _thread;
        private readonly IQueue _queue;

        /// <summary>
        /// Create a worker thread with the default queue.
        /// </summary>
        public UserWorkerThread() 
            : this(new DefaultQueue())
        {}

        /// <summary>
        /// Creates a worker thread with the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public UserWorkerThread(IQueue queue) 
            : this(queue, "UserWorkerThread-" + GetNextThreadId())
        {}

        /// <summary>
        /// Creates a worker thread with the specified name.
        /// </summary>
        /// /// <param name="threadName"></param>
        public UserWorkerThread(string threadName)
            : this(new DefaultQueue(), threadName)
        {}

        /// <summary>
        /// Creates a worker thread.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public UserWorkerThread(IQueue queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            _queue = queue;
            _thread = new Thread(RunThread);
            _thread.Name = threadName;
            _thread.IsBackground = isBackground;
            _thread.Priority = priority;
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
        public void Queue(WaitCallback callback)
        {
            _queue.Enqueue(() => callback(null));
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
