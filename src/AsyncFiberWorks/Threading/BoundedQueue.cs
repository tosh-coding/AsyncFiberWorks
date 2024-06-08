using System;
using System.Collections.Generic;
using System.Threading;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Queue with bounded capacity.  Will throw exception if capacity does not recede prior to wait time.
    /// </summary>
    public class BoundedQueue : IDedicatedConsumerThreadWork
    {
        private readonly object _lock = new object();
        private readonly IExecutorBatch _executorBatch;
        private readonly IExecutor _executorSingle;

        private bool _running = true;

        private List<Action> _actions = new List<Action>();
        private List<Action> _toPass = new List<Action>();

        ///<summary>
        /// Creates a bounded queue with a custom executor.
        ///</summary>
        ///<param name="executorBatch"></param>
        ///<param name="executorSingle"></param>
        public BoundedQueue(IExecutorBatch executorBatch, IExecutor executorSingle)
        {
            MaxDepth = -1;
            _executorBatch = executorBatch;
            _executorSingle = executorSingle;
        }

        ///<summary>
        /// Creates a bounded queue with a simple executor.
        ///</summary>
        public BoundedQueue()
            : this(SimpleExecutorBatch.Instance, SimpleExecutor.Instance)
        {
        }

        /// <summary>
        /// Max number of actions to be queued.
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Max time to wait for space in the queue.
        /// </summary>
        public int MaxEnqueueWaitTimeInMs { get; set; }

        /// <summary>
        /// Enqueue action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            lock (_lock)
            {
                if (SpaceAvailable(1))
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

        private bool SpaceAvailable(int toAdd)
        {
            if (!_running)
            {
                return false;
            }
            while (MaxDepth > 0 && _actions.Count + toAdd > MaxDepth)
            {
                if (MaxEnqueueWaitTimeInMs <= 0)
                {
                    throw new QueueFullException(_actions.Count);
                }
                Monitor.Wait(_lock, MaxEnqueueWaitTimeInMs);
                if (!_running)
                {
                    return false;
                }
                if (MaxDepth > 0 && _actions.Count + toAdd > MaxDepth)
                {
                    throw new QueueFullException(_actions.Count);
                }
            }
            return true;
        }

        private List<Action> DequeueAll()
        {
            lock (_lock)
            {
                if (ReadyToDequeue())
                {
                    ListUtil.Swap(ref _actions, ref _toPass);
                    _actions.Clear();

                    Monitor.PulseAll(_lock);
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
            if (!_running)
            {
                return false;
            }
            return true;
        }

        private bool ExecuteNextBatch()
        {
            var toExecute = DequeueAll();
            if (toExecute == null)
            {
                return false;
            }
            _executorBatch.Execute(toExecute, _executorSingle);
            return true;
        }
    }
}