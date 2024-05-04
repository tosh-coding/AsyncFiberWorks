using AsyncFiberWorks.Core;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// A consumer queue that executes queued tasks in sequence.
    /// A loop process is started internally. It is stopped by Dispose.
    /// </summary>
    public class LoopRunningAsyncFiber : IAsyncFiber, IDisposable
    {
        readonly ConcurrentQueue<Func<Task>> _queue = new ConcurrentQueue<Func<Task>>();
        bool _stopped;

        /// <summary>
        /// Create a fiber.
        /// </summary>
        public LoopRunningAsyncFiber()
        {
            Run();
        }

        bool IsStopped
        {
            get
            {
                lock (_queue)
                {
                    return _stopped;
                }
            }
            set
            {
                lock (_queue)
                {
                    _stopped = value;
                }
            }
        }

        /// <summary>
        /// Start the loop.
        /// </summary>
        public async void Run()
        {
            await Task.Yield();
            while (!IsStopped)
            {
                while (_queue.TryDequeue(out var func))
                {
                    await Task.Yield();
                    await func().ConfigureAwait(false);
                }
                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stop the loop.
        /// </summary>
        public void Dispose()
        {
            IsStopped = true;
        }

        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="func">A function that returns a task.</param>
        public void Enqueue(Func<Task> func)
        {
            _queue.Enqueue(func);
        }
    }
}
