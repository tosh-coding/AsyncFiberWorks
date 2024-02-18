using System;
using System.Collections.Concurrent;

namespace Retlang.Core
{
    /// <summary>
    /// This is a fiber that needs to be pumped manually.
    /// Queued actions are added to the pending list.
    /// Consume them by periodically calling methods for execution.
    /// Periodically call a method for execution. They are executed on their calling thread.
    /// </summary>
    public sealed class StubFiberSlim : IExecutionContext
    {
        private readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
        private readonly IExecutor _executor;

        /// <summary>
        /// Create a stub fiber with the default executor.
        /// </summary>
        public StubFiberSlim()
            : this(new DefaultExecutor())
        {
        }

        /// <summary>
        /// Create a stub fiber with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public StubFiberSlim(IExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _pending.Enqueue(action);
        }

        /// <summary>
        /// Execute until there are no more pending actions.
        /// </summary>
        public int ExecuteAll()
        {
            int count = 0;
            while (_pending.TryDequeue(out var toExecute))
            {
                _executor.Execute(toExecute);
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// Execute only what is pending now.
        /// </summary>
        public int ExecuteOnlyPendingNow()
        {
            int count = _pending.Count;
            int ret = count;
            while (_pending.TryDequeue(out var toExecute))
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
