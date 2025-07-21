using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Busy waits on lock to execute.  Can improve performance in certain situations.
    /// </summary>
    public class BusyWaitQueue : IDedicatedConsumerThreadWork
    {
        private readonly object _lock = new object();
        private readonly IHookOfBatch _hookOfBatch;
        private readonly IExecutor _executorSingle;
        private readonly int _spinsBeforeTimeCheck;
        private readonly int _msBeforeBlockingWait;

        private bool _running = true;

        private List<Action> _actions = new List<Action>();
        private List<Action> _toPass = new List<Action>();

        /// <summary>
        /// BusyWaitQueue with custom executor.
        /// </summary>
        /// <param name="spinsBeforeTimeCheck"></param>
        /// <param name="msBeforeBlockingWait"></param>
        /// <param name="hookOfBatch"></param>
        /// <param name="executorSingle">The executor for each operation.</param>
        public BusyWaitQueue(int spinsBeforeTimeCheck, int msBeforeBlockingWait, IHookOfBatch hookOfBatch, IExecutor executorSingle)
        {
            _hookOfBatch = hookOfBatch;
            _executorSingle = executorSingle;
            _spinsBeforeTimeCheck = spinsBeforeTimeCheck;
            _msBeforeBlockingWait = msBeforeBlockingWait;
        }

        ///<summary>
        /// BusyWaitQueue with a simple executor.
        ///</summary>
        public BusyWaitQueue(int spinsBeforeTimeCheck, int msBeforeBlockingWait)
            : this(spinsBeforeTimeCheck, msBeforeBlockingWait, NoneHookOfBatch.Instance, SimpleExecutor.Instance)
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
            var spins = 0;
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                try
                {
                    while (!Monitor.TryEnter(_lock)) {}

                    if (!_running) break;
                    var toReturn = TryDequeue();
                    if (toReturn != null) return toReturn;

                    if (TryBlockingWait(stopwatch, ref spins))
                    {
                        if (!_running) break;
                        toReturn = TryDequeue();
                        if (toReturn != null) return toReturn;
                    }
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
                Thread.Yield();
            }

            return null;
        }

        private bool TryBlockingWait(Stopwatch stopwatch, ref int spins)
        {
            if (spins++ < _spinsBeforeTimeCheck)
            {
                return false;
            }

            spins = 0;
            if (stopwatch.ElapsedMilliseconds > _msBeforeBlockingWait)
            {
                Monitor.Wait(_lock);
                stopwatch.Restart();
                return true;
            }

            return false;
        }

        private List<Action> TryDequeue()
        {
            if (_actions.Count > 0)
            {
                ListUtil.Swap(ref _actions, ref _toPass);
                _actions.Clear();
                return _toPass;
            }

            return null;
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
