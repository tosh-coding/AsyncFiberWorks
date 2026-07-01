using System;
using System.Collections.Generic;
using System.Threading;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Default implementation.
    /// </summary>
    public class DefaultQueue : IDedicatedConsumerThreadWorkQueue
    {
        private readonly object _lock = new object();
        private readonly IHookOfBatch _hookOfBatch;
        private readonly IActionExceptionHandler _exceptionHandler;

        private bool _running = true;

        private List<(WaitCallback, object)> _actions;
        private List<(WaitCallback, object)> _toPass;

        /// <summary>
        /// Initializes a new instance of the queue with the specified exception handler.
        /// </summary>
        /// <param name="hookOfBatch"></param>
        /// <param name="exceptionHandler">Handler used to process exceptions thrown by queued actions.</param>
        /// <param name="initialCapacity"></param>
        /// <exception cref="ArgumentOutOfRangeException">initialCapacity must be greater than or equal to 1.</exception>
        public DefaultQueue(IHookOfBatch hookOfBatch, IActionExceptionHandler exceptionHandler, int initialCapacity = 4)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }
            _hookOfBatch = hookOfBatch;
            _exceptionHandler = exceptionHandler;
            _actions = new List<(WaitCallback, object)>(initialCapacity);
            _toPass = new List<(WaitCallback, object)>(initialCapacity);
        }

        ///<summary>
        /// Initializes a new instance of the queue without a custom exception handler.
        ///</summary>
        public DefaultQueue()
            : this(NoneHookOfBatch.Instance, null)
        {
        }

        /// <summary>
        /// Enqueue action.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        /// <param name="state">An object containing information to be used by the action. </param>
        public void Enqueue(WaitCallback action, object state)
        {
            lock (_lock)
            {
                _actions.Add((action, state));
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

        private List<(WaitCallback, object)> DequeueAll()
        {
            lock (_lock)
            {
                if (ReadyToDequeue())
                {
                    Swap(ref _actions, ref _toPass);
                    _actions.Clear();
                    return _toPass;
                }
                return null;
            }
        }

        private static void Swap(ref List<(WaitCallback, object)> a, ref List<(WaitCallback, object)> b)
        {
            var tmp = a;
            a = b;
            b = tmp;
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
                try
                {
                    action.Item1?.Invoke(action.Item2);
                }
                catch (Exception ex)
                {
                    try
                    {
                        _exceptionHandler?.Handle(ex);
                    }
                    catch { }
                }
            }
            _hookOfBatch.OnAfterExecute(toExecute.Count);
            return true;
        }
    }
}
