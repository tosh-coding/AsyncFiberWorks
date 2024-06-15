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
        private readonly IExecutorBatch _executorBatch;
        private readonly IExecutor _executorSingle;

        private bool _running = true;

        private List<Action> _actions = new List<Action>();
        private List<Action> _toPass = new List<Action>();

        /// <summary>
        /// Default queue with custom executor
        /// </summary>
        /// <param name="executorBatch"></param>
        /// <param name="executorSingle">The executor for each operation.</param>
        public DefaultQueue(IExecutorBatch executorBatch, IExecutor executorSingle)
        {
            _executorBatch = executorBatch;
            _executorSingle = executorSingle;
        }

        ///<summary>
        /// Default queue with a simple executor
        ///</summary>
        public DefaultQueue()
            : this(SimpleExecutorBatch.Instance, SimpleExecutor.Instance)
        {
        }

        /// <summary>
        /// Enqueue action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            if (_executorSingle != null)
            {
                lock (_lock)
                {
                    _actions.Add(() => _executorSingle.Execute(action));
                    Monitor.PulseAll(_lock);
                }
            }
            else
            {
                lock (_lock)
                {
                    _actions.Add(action);
                    Monitor.PulseAll(_lock);
                }
            }
        }

        /// <summary>
        /// Execute actions until stopped.
        /// </summary>
        public void Run()
        {
            _running = true;
            while (ExecuteNextBatch()) { }
        }

        /// <summary>
        /// Stop consuming actions.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
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
        /// Remove all actions and execute.
        /// </summary>
        /// <returns></returns>
        public bool ExecuteNextBatch()
        {
            var toExecute = DequeueAll();
            if (toExecute == null)
            {
                return false;
            }
            _executorBatch.Execute(toExecute);
            return true;
        }
    }
}