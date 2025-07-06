using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// A worker thread.
    /// </summary>
    public sealed class UserWorkerThread
    {
        private static readonly object _staticLockObj = new object();
        private static int _staticThreadCounter = 0;

        private readonly Thread _thread;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;

        /// <summary>
        /// Creates a worker thread.
        /// </summary>
        /// <param name="work">The work that the thread performs.</param>
        /// <param name="threadName">Thread name. If null, auto naming.</param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public UserWorkerThread(Action work, string threadName = null, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }
            if (threadName == null)
            {
                threadName = CreateThreadName();
            }

            _thread = new Thread(() => RunThread(work));
            _thread.Name = threadName;
            _thread.IsBackground = isBackground;
            _thread.Priority = priority;
            _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// The dedicated thread.
        /// </summary>
        public Thread Thread
        {
            get { return _thread; }
        }

        private static string CreateThreadName()
        {
            int count;
            lock (_staticLockObj)
            {
                _staticThreadCounter += 1;
                count = _staticThreadCounter;
            }
            return $"UserWorkerThread-{count}";
        }

        private void RunThread(Action work)
        {
            try
            {
                work.Invoke();
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
        public Task JoinAsync()
        {
            return _taskCompletionSource.Task;
        }
    }
}
