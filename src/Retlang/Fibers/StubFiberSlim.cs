using System;
using System.Collections.Concurrent;
using System.Threading;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// StubFiberSlim does not use a backing thread or a thread pool for execution. Actions are added to pending
    /// lists for execution. These actions can be executed synchronously by the calling thread.
    /// </summary>
    public class StubFiberSlim : IFiberSlim, IConsumingContext
    {
        private readonly BlockingCollection<Action> _pending = new BlockingCollection<Action>();
        private readonly IExecutor _executor;

        /// <summary>
        /// Create a stub fiber with the default executor.
        /// </summary>
        public StubFiberSlim()
            : this(new DefaultExecutor())
        {}

        /// <summary>
        /// Create a stub fiber with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public StubFiberSlim(IExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Create a stub fiber with the default executor, and call the Start method.
        /// </summary>
        public static StubFiberSlim StartNew()
        {
            var fiber = new StubFiberSlim();
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Create a stub fiber with the specified executor, and call the Start method.
        /// </summary>
        public static StubFiberSlim StartNew(IExecutor executor)
        {
            var fiber = new StubFiberSlim(executor);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// No Op
        /// </summary>
        public void Start()
        {}

        /// <summary>
        /// Destroy the instance.
        /// </summary>
        public void Dispose()
        {
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
        /// Execute all actions in the pending list.  If any of the executed actions enqueue more actions, execute those as well.
        /// </summary>
        public void ExecuteAllPendingUntilEmpty()
        {
            while (_pending.TryTake(out var toExecute))
            {
                _executor.Execute(toExecute);
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
                _executor.Execute(toExecute);

                count -= 1;
                if (count <= 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Execute actions until canceled.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException">Canceled.</exception>
        public void ExecuteUntilCanceled(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var toExecute = _pending.Take(cancellationToken);
                _executor.Execute(toExecute);
            }
        }
    }
}
