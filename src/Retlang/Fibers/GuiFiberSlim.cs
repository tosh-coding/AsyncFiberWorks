using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Retlang.Core;

namespace Retlang.Fibers
{
    ///<summary>
    /// Allows interaction with Windows Forms.  Transparently moves actions onto the Form's thread.
    ///</summary>
    public class GuiFiberSlim : IFiberSlim
    {
        private readonly object _lock = new object();
        private readonly IExecutionContext _executionContext;
        private readonly IExecutor _executor;
        private readonly List<Action> _queue = new List<Action>();

        private volatile ExecutionState _started = ExecutionState.Created;

        /// <summary>
        /// Creates an instance.
        /// </summary>
        public GuiFiberSlim(IExecutionContext executionContext, IExecutor executor)
        {
            _executionContext = executionContext;
            _executor = executor;
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            if (_started == ExecutionState.Stopped)
            {
                return;
            }

            if (_started == ExecutionState.Created)
            {
                lock (_lock)
                {
                    if (_started == ExecutionState.Created)
                    {
                        _queue.Add(action);
                        return;
                    }
                }
            }

            _executionContext.Enqueue(() => _executor.Execute(action));
        }

        /// <summary>
        /// <see cref="IFiberSlim.Start()"/>
        /// </summary>
        public void Start()
        {
            if (_started == ExecutionState.Running)
            {
                throw new ThreadStateException("Already Started");
            }

            lock (_lock)
            {
                var actions = _queue.ToList();
                _queue.Clear();
                if (actions.Count > 0)
                {
                    _executionContext.Enqueue(() => _executor.Execute(actions));
                }
                _started = ExecutionState.Running;
            }
        }

        /// <summary>
        /// <see cref="IDisposable.Dispose()"/>
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Stops the fiber.
        /// </summary>
        public void Stop()
        {
            _started = ExecutionState.Stopped;
        }
    }
}