using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Retlang.Core
{
    /// <summary>
    /// StubWorkerThread is an alternative implementation of worker threads for the thread pool.
    /// Actions are added to pending lists for execution. These actions can be executed
    /// synchronously by the calling thread.
    /// </summary>
    public sealed class StubWorkerThread : IThreadPool, IConsumingContext
    {
        private readonly BlockingCollection<Action> _pending = new BlockingCollection<Action>();
        private readonly IExecutor _executor;

        /// <summary>
        /// Create a stub worker thread with the default executor.
        /// </summary>
        public StubWorkerThread()
            : this(new DefaultExecutor())
        {}

        /// <summary>
        /// Create a stub worker thread with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public StubWorkerThread(IExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Queue(WaitCallback callback)
        {
            _pending.Add(() => callback(null));
        }

        /// <summary>
        /// Execute all actions in the pending list.
        /// If any of the executed actions enqueue more actions, execute those as well.
        /// </summary>
        public int ExecuteAllPendingUntilEmpty()
        {
            int count = 0;
            while (_pending.TryTake(out var toExecute))
            {
                _executor.Execute(toExecute);
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// Execute all actions in the pending list.
        /// </summary>
        public int ExecuteAllPending()
        {
            int count = _pending.Count;
            int ret = count;
            while (_pending.TryTake(out var toExecute))
            {
                _executor.Execute(toExecute);

                count -= 1;
                if (count <= 0)
                {
                    break;
                }
            }
            return ret;
        }
    }
}
