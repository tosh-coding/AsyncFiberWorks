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
        private readonly Scheduler _scheduler = new Scheduler();
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
        public void Enqueue(Action action)
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
        /// Number of scheduled actions.
        /// </summary>
        public int NumScheduledActions
        {
            get { return _scheduler.Count; }
        }

        /// <summary>
        /// Register a timer. So that it stops together when the fiber is disposed.
        /// </summary>
        /// <param name="timer"></param>
        public void RegisterSchedule(IDisposable timer)
        {
            _scheduler.Add(timer);
        }

        /// <summary>
        /// Deregister a timer.
        /// </summary>
        /// <param name="timer"></param>
        public void DeregisterSchedule(IDisposable timer)
        {
            _scheduler.Remove(timer);
        }

        /// <summary>
        /// <see cref="IFiber.Start()"/>
        /// </summary>
        public virtual void Start()
        {
            _fiber.Start();
        }

        /// <summary>
        /// <see cref="IDisposable.Dispose()"/>
        /// </summary>
        public virtual void Dispose()
        {
            _scheduler.Dispose();
            _subscriptions.Dispose();
            _fiber.Dispose();
        }
    }
}
