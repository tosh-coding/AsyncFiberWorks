using System;
using System.Threading;
using System.Threading.Tasks;

namespace Retlang.Core
{
    /// <summary>
    /// A thread pool implementation with only one worker thread.
    /// </summary>
    public sealed class UserWorkerThread : IConsumerThread
    {
        private static int THREAD_COUNT;
        private readonly Thread _thread;
        private readonly IConsumerQueueForThread _queue;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;

        /// <summary>
        /// Creates a worker thread.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public UserWorkerThread(IConsumerQueueForThread queue, string threadName = null, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            if (threadName == null)
            {
                threadName = "UserWorkerThread-" + GetNextThreadId();
            }

            _queue = queue;
            _thread = new Thread(RunThread);
            _thread.Name = threadName;
            _thread.IsBackground = isBackground;
            _thread.Priority = priority;
            _taskCompletionSource = new TaskCompletionSource<bool>();
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
            try
            {
                _queue.Run();
            }
            finally
            {
                _taskCompletionSource.SetResult(true);
            }
        }

        /// <summary>
        /// Start the thread.
        /// </summary>
        public void Start()
        {
            _thread.Start();
        }

        /// <summary>
        /// Returns a task waiting for thread termination.
        /// </summary>
        public Task Join()
        {
            return _taskCompletionSource.Task;
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Stop()
        {
            _queue.Stop();
        }
    }
}
