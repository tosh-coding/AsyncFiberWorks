using System;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Fiber with disposables of subscription and scheduling.
    /// </summary>
    public class FiberWithDisposableList : IFiber
    {
        private readonly Subscriptions _subscriptions = new Subscriptions();
        private readonly IFiberSlim _fiber;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        /// <param name="fiber"></param>
        public FiberWithDisposableList(IFiberSlim fiber)
        {
            _fiber = fiber;
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public virtual void Enqueue(Action action)
        {
            _fiber.Enqueue(action);
        }

        ///<summary>
        /// Register subscription to be unsubcribed from when the fiber is disposed.
        ///</summary>
        ///<param name="toAdd"></param>
        public void RegisterSubscription(IDisposable toAdd)
        {
            _subscriptions.Add(toAdd);
        }

        ///<summary>
        /// Deregister a subscription.
        ///</summary>
        ///<param name="toRemove"></param>
        ///<returns></returns>
        public bool DeregisterSubscription(IDisposable toRemove)
        {
            return _subscriptions.Remove(toRemove);
        }

        ///<summary>
        /// Number of subscriptions.
        ///</summary>
        public int NumSubscriptions
        {
            get { return _subscriptions.Count; }
        }

        /// <summary>
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public virtual void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
