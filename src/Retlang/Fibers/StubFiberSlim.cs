using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Retlang.Fibers
{
    /// <summary>
    /// StubFiberSlim does not use a backing thread or a thread pool for execution. Actions are added to pending
    /// lists for execution. These actions can be executed synchronously by the calling thread.
    /// </summary>
    public class StubFiberSlim : IFiberSlim
    {
        private readonly BlockingCollection<Action> _pending = new BlockingCollection<Action>();

        /// <summary>
        /// Construct new instance.
        /// </summary>
        public StubFiberSlim()
        {}

        /// <summary>
        /// No Op
        /// </summary>
        public void Start()
        {}

        /// <summary>
        /// Clears all pending actions.
        /// </summary>
        public void Dispose()
        {
            while (_pending.TryTake(out _))
            {}
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _pending.Add(action);
        }

        /// <summary>
        /// Number of pending actions.
        /// </summary>
        public int NumPendingActions
        {
            get { return _pending.Count; }
        }

        /// <summary>
        /// Execute all actions in the pending list.  If any of the executed actions enqueue more actions, execute those as well.
        /// </summary>
        public void ExecuteAllPendingUntilEmpty()
        {
            while (_pending.TryTake(out var toExecute))
            {
                toExecute();
            }
        }

        /// <summary>
        /// Execute all actions in the pending list.
        /// </summary>
        public void ExecuteAllPending()
        {
            int count = _pending.Count;
            while (_pending.TryTake(out var toExecute))
            {
                toExecute();

                count -= 1;
                if (count <= 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Execute actions until cancelled.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException">Cancelled.</exception>
        public void ExecuteUntilCancelled(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var toExecute = _pending.Take(cancellationToken);
                toExecute();
            }
        }
    }
}
