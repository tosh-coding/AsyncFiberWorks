using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// A consumer queue that executes queued tasks in sequence.
    /// A loop process is started internally. It is stopped by Dispose.
    /// </summary>
    public class SemaphoreLoopAsyncFiber : IAsyncFiber, IDisposable
    {
        readonly object _lockObj = new object();
        readonly Queue<Func<Task>> _queue = new Queue<Func<Task>>();
        readonly SemaphoreSlim _sem = new SemaphoreSlim(0);
        bool _stopped;

        /// <summary>
        /// Create a fiber.
        /// </summary>
        public SemaphoreLoopAsyncFiber()
        {
            Run();
        }

        /// <summary>
        /// Dispose a fiber.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _sem.Release(1);
            _sem.Dispose();
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

        bool TryDequeue(out Func<Task> result)
        {
            lock (_lockObj)
            {
                if (_queue.Count > 0)
                {
                    result = _queue.Dequeue();
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Start the loop.
        /// </summary>
        async void Run()
        {
            await Task.Yield();
            while (!IsStopped)
            {
                while (TryDequeue(out var func))
                {
                    await Task.Yield();
                    await func().ConfigureAwait(false);
                }
                await _sem.WaitAsync(1).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stop the loop.
        /// </summary>
        void Stop()
        {
            IsStopped = true;
        }

        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="func">A function that returns a task.</param>
        public void Enqueue(Func<Task> func)
        {
            lock (_lockObj)
            {
                _queue.Enqueue(func);
                if (_queue.Count <= 1)
                {
                    _sem.Release(1);
                }
            }
        }
    }
}
