using Retlang.Core;
using System;
using System.Collections.Generic;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber implementation backed by shared threads. Mainly thread pool.
    /// </summary>
    public class PoolFiber : FiberWithDisposableList
    {
        private readonly object _lock = new object();
        private readonly IFiberSlim _fiber;

        private List<Action> _queue = new List<Action>();

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="poolFiber"></param>
        private PoolFiber(PoolFiberSlim poolFiber)
            : base(poolFiber, new Subscriptions())
        {
            _fiber = poolFiber;
        }

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="executor"></param>
        public PoolFiber(IThreadPool pool, IExecutor executor)
            : this(new PoolFiberSlim(pool, executor))
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool.
        /// </summary>
        public PoolFiber(IExecutor executor) 
            : this(DefaultThreadPool.Instance, executor)
        {
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and default executor.
        /// </summary>
        public PoolFiber() 
            : this(DefaultThreadPool.Instance, new DefaultExecutor())
        {
        }

        /// <summary>
        /// Create a pool fiber with the specified thread pool and specified executor,
        /// and call the Start method.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public static PoolFiber StartNew(IThreadPool pool, IExecutor executor)
        {
            var fiber = new PoolFiber(pool, executor);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool, and call the Start method.
        /// </summary>
        /// <param name="executor"></param>
        /// <returns></returns>
        public static PoolFiber StartNew(IExecutor executor)
        {
            var fiber = new PoolFiber(executor);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Create a pool fiber with the default thread pool and default executor,
        /// and call the Start method.
        /// </summary>
        /// <returns></returns>
        public static PoolFiber StartNew()
        {
            var fiber = new PoolFiber();
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Start consuming actions.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_queue == null)
                {
                    return;
                }
                foreach (var action in _queue)
                {
                    _fiber.Enqueue(action);
                }
                _queue = null;
            }
        }

        /// <summary>
        /// Clears all subscriptions, scheduled, and pending actions.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        /// <summary>
        /// <see cref="IExecutionContext.Enqueue(Action)"/>
        /// </summary>
        /// <param name="action"></param>
        public override void Enqueue(Action action)
        {
            lock (_lock)
            {
                if (_queue == null)
                {
                    _fiber.Enqueue(action);
                }
                else
                {
                    _queue.Add(action);
                }
            }
        }
    }
}