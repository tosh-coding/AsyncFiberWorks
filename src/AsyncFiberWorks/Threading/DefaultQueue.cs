using System;
using System.Collections.Generic;
using System.Threading;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Default implementation.
    /// </summary>
    public class DefaultQueue : IDedicatedConsumerThreadWork
    {
        private readonly object _lock = new object();
        private readonly IHookOfBatch _hookOfBatch;
        private readonly IExecutor _executorSingle;

        private bool _running = true;

        private List<Action> _actions;
        private List<Action> _toPass;

        /// <summary>
        /// Default queue with custom executor
        /// </summary>
        /// <param name="hookOfBatch"></param>
        /// <param name="executorSingle">The executor for each operation.</param>
        /// <param name="initialCapacity"></param>
        /// <exception cref="ArgumentOutOfRangeException">initialCapacity must be greater than or equal to 1.</exception>
        public DefaultQueue(IHookOfBatch hookOfBatch, IExecutor executorSingle, int initialCapacity = 4)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }
            _hookOfBatch = hookOfBatch;
            _executorSingle = executorSingle ?? SimpleExecutor.Instance;
            _actions = new List<Action>(initialCapacity);
            _toPass = new List<Action>(initialCapacity);
        }

        ///<summary>
        /// Default queue with a simple executor
        ///</summary>
        public DefaultQueue()
            : this(NoneHookOfBatch.Instance, SimpleExecutor.Instance)
        {
        }

        /// <summary>
        /// Enqueue action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            lock (_lock)
            {
                _actions.Add(action);
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Stop consuming actions.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_running)
                {
                    return;
                }
                _running = false;
                Monitor.PulseAll(_lock);
            }
        }

        private List<Action> DequeueAll()
        {
            lock (_lock)
            {
                if (ReadyToDequeue())
                {
                    ListUtil.Swap(ref _actions, ref _toPass);
                    _actions.Clear();
                    return _toPass;
                }
                return null;
            }
        }

        private bool ReadyToDequeue()
        {
            while (_actions.Count == 0 && _running)
            {
                Monitor.Wait(_lock);
            }
            return _running;
        }

        /// <summary>
        /// Perform pending actions.
        /// </summary>
        /// <returns>Is it still in operation?</returns>
        public bool ExecuteNextBatch()
        {
            var toExecute = DequeueAll();
            if (toExecute == null)
            {
                return false;
            }
            _hookOfBatch.OnBeforeExecute(toExecute.Count);
            foreach (var action in toExecute)
            {
                _executorSingle.Execute(action);
            }
            _hookOfBatch.OnAfterExecute(toExecute.Count);
            return true;
        }
    }
}
