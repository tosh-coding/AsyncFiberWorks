using System;
using System.Windows.Threading;
using Retlang.Core;
using Retlang.Fibers;

namespace WpfExample
{
    /// <summary>
    /// Adapts Dispatcher to a Fiber. Transparently moves actions onto the Dispatcher thread.
    /// </summary>
    public class DispatcherFiber : IFiber
    {
        private readonly IFiberSlim _fiber;
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="priority">The priority.</param>
        /// <param name="executor">The executor.</param>
        public DispatcherFiber(Dispatcher dispatcher, DispatcherPriority priority, IExecutor executor)
        {
            _fiber = new PoolFiberSlim(new DispatcherAdapter(dispatcher, priority), executor);
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="executor">The priority.</param>
        public DispatcherFiber(Dispatcher dispatcher, IExecutor executor)
            : this(dispatcher, DispatcherPriority.Normal, executor)
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="priority">The priority.</param>
        public DispatcherFiber(Dispatcher dispatcher, DispatcherPriority priority)
            : this(dispatcher, priority, new DefaultExecutor())
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        public DispatcherFiber(Dispatcher dispatcher)
            : this(dispatcher, new DefaultExecutor())
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread of the
        /// current dispatcher.
        /// </summary>
        /// <param name="priority">The priority.</param>
        public DispatcherFiber(DispatcherPriority priority)
            : this(Dispatcher.CurrentDispatcher, priority, new DefaultExecutor())
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread of the
        /// current dispatcher.
        /// </summary>
        public DispatcherFiber()
            : this(Dispatcher.CurrentDispatcher, new DefaultExecutor())
        {
        }

        /// <summary>
        /// <see cref="IExecutionContext.Enqueue(Action)"/>
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _fiber.Enqueue(action);
        }

        /// <summary>
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public void Stop()
        {
            Dispose();
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistryGetter.FallbackDisposer"/>
        /// </summary>
        public ISubscriptionRegistry FallbackDisposer
        {
            get { return _subscriptions; }
        }

        /// <summary>
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
        }
    }
}