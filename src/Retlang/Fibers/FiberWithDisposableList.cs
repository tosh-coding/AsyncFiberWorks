using System;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber with disposables of subscription and scheduling.
    /// </summary>
    public class FiberWithDisposableList : IFiber
    {
        private readonly Subscriptions _subscriptions;
        private readonly IFiberSlim _fiber;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="subscriptions"></param>
        public FiberWithDisposableList(IFiberSlim fiber, Subscriptions subscriptions)
        {
            _fiber = fiber;
            _subscriptions = subscriptions;
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public virtual void Enqueue(Action action)
        {
            _fiber.Enqueue(action);
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
        public virtual void Dispose()
        {
            _subscriptions?.Dispose();
        }
    }
}
