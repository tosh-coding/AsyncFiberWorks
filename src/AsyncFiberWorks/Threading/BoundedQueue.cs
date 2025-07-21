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
        private readonly IHookOfBatch _hookOfBatch;
        private readonly IExecutor _executorSingle;

        private bool _running = true;

        private List<Action> _actions = new List<Action>();
        private List<Action> _toPass = new List<Action>();

        /// <summary>
        /// Creates a bounded queue with a custom executor.
        /// </summary>
        /// <param name="hookOfBatch"></param>
        /// <param name="executorSingle">The executor for each operation.</param>
        public BoundedQueue(IHookOfBatch hookOfBatch, IExecutor executorSingle)
        {
            MaxDepth = -1;
            _hookOfBatch = hookOfBatch;
            _executorSingle = executorSingle;
        }

        ///<summary>
        /// Creates a bounded queue with a simple executor.
        ///</summary>
        public BoundedQueue()
            : this(NoneHookOfBatch.Instance, SimpleExecutor.Instance)
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
            if (_executorSingle != null)
            {
                lock (_lock)
                {
                    if (SpaceAvailable(1))
                    {
                        _actions.Add(() => _executorSingle.Execute(action));
                        Monitor.PulseAll(_lock);
                    }
                }
            }
            else
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

        /// <summary>
        /// Perform pending actions.
        /// </summary>
        /// <returns>Still in operation. False if already stopped.</returns>
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
                action();
            }
            _hookOfBatch.OnAfterExecute(toExecute.Count);
            return true;
        }
    }
}
