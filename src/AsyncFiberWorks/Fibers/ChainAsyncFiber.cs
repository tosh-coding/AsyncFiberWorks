using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// A consumer queue that executes queued tasks in sequence.
    /// </summary>
    public class ChainAsyncFiber : IAsyncFiber
    {
        readonly object _lockObj = new object();
        readonly Queue<Func<Task>> _queue = new Queue<Func<Task>>();
        bool _running = false;
        IAsyncExecutor _executor;

        /// <summary>
        /// Create a fiber.
        /// </summary>
        /// <param name="executor"></param>
        public ChainAsyncFiber(IAsyncExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Create a fiber.
        /// </summary>
        public ChainAsyncFiber()
            : this(AsyncSimpleExecutor.Instance)
        {
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
        /// Start consumption.
        /// </summary>
        async void Run()
        {
            await Task.Yield();
            while (TryDequeue(out var func))
            {
                await Task.Yield();
                await _executor.Execute(func).ConfigureAwait(false);
            }
            lock (_lockObj)
            {
                _running = false;
            }
        }

        /// <summary>
        /// Enqueue a task.
        /// </summary>
        /// <param name="func">A function that returns a task.</param>
        public void Enqueue(Func<Task> func)
        {
            bool startedNow = false;
            lock (_lockObj)
            {
                _queue.Enqueue(func);
                if (!_running)
                {
                    _running = true;
                    startedNow = true;
                }
            }

            if (startedNow)
            {
                Run();
            }
        }
    }
}
