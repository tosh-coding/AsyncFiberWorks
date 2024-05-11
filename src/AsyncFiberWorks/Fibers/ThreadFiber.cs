using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Fiber implementation backed by a dedicated thread.
    /// </summary>
    public class ThreadFiber : IFiber, IDisposable, IAsyncExecutionContext
    {
        private readonly IDedicatedConsumerThreadWork _queue;
        private readonly UserWorkerThread _workerThread;
        private bool _stopped = false;

        /// <summary>
        /// Create a thread fiber with the default queue.
        /// </summary>
        public ThreadFiber()
            : this(new DefaultQueue())
        {
        }

        /// <summary>
        /// Create a thread fiber with the specified queue.
        /// </summary>
        /// <param name="queue"></param>
        public ThreadFiber(IDedicatedConsumerThreadWork queue)
            : this(queue, null)
        {
        }

        /// <summary>
        /// Create a thread fiber with the specified thread name.
        /// </summary>
        /// <param name="threadName"></param>
        public ThreadFiber(string threadName)
            : this(new DefaultQueue(), threadName)
        {
        }

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public ThreadFiber(IDedicatedConsumerThreadWork queue, string threadName, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            _queue = queue;
            _workerThread = new UserWorkerThread(queue, threadName, isBackground, priority);
            _workerThread.Start();
        }

        /// <summary>
        /// Creates a thread fiber.
        /// </summary>
        /// <param name="consumerThread">A consumer thread.</param>
        public ThreadFiber(UserWorkerThread consumerThread)
        {
            _workerThread = consumerThread;
            _workerThread.Start();
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Stop()
        {
            if (!_stopped)
            {
                _workerThread.Stop();
                _stopped = true;
            }
        }

        /// <summary>
        /// Stop the thread.
        /// </summary>
        public void Dispose()
        {
            Stop();
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
        /// Enqueue a single task.
        /// </summary>
        /// <param name="func">Task generator. This is done after a pause in the fiber. The generated task is monitored and takes action to resume after completion.</param>
        public void Enqueue(Func<Task<Action>> func)
        {
            this.Enqueue(() =>
            {
                var tcs = new TaskCompletionSource<Action>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task.Run(async () =>
                {
                    Action resumingAction = default;
                    try
                    {
                        resumingAction = await func.Invoke();
                    }
                    finally
                    {
                        tcs.SetResult(resumingAction);
                    }
                });

                // This is in a dedicated thread. Blocking OK.
                tcs.Task.Wait();
                var act = tcs.Task.Result;
                act?.Invoke();
            });
        }
    }
}
