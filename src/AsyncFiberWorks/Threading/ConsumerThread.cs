using AsyncFiberWorks.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Consumer thread for task queue jobs.
    /// </summary>
    public sealed class ConsumerThread : IExecutionContext, IDisposable
    {
        private static readonly object _staticLockObj = new object();
        private static int _staticThreadCounter = 0;

        private readonly Thread _thread;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;
        private readonly IDedicatedConsumerThreadWork _queue;

        /// <summary>
        /// Create a consumer thread.
        /// </summary>
        /// <param name="queue">Queue to receive tasks to be executed by the thread. If null, some kind of queue is used.</param>
        /// <param name="threadName">Thread name. If null, auto naming.</param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static ConsumerThread StartNew(IDedicatedConsumerThreadWork queue = null, string threadName = null, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            if (queue == null)
            {
                queue = new DefaultQueue();
            }
            var thread = new ConsumerThread(queue, threadName, isBackground, priority);
            thread.Start();
            return thread;
        }

        /// <summary>
        /// Create a consumer thread with the specified queue.
        /// </summary>
        /// <param name="queue">Queue to receive tasks to be executed by the thread.</param>
        /// <param name="threadName">Thread name. If null, auto naming.</param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ConsumerThread(IDedicatedConsumerThreadWork queue, string threadName = null, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }
            if (threadName == null)
            {
                threadName = CreateThreadName();
            }

            _queue = queue;
            _thread = new Thread(() => RunThread(queue.Run));
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
        /// Stop the thread.
        /// </summary>
        public void Stop()
        {
            _queue.Stop();
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Returns a task waiting for thread termination.
        /// </summary>
        public Task JoinAsync()
        {
            return _taskCompletionSource.Task;
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Enqueue(action);
        }
    }
}
