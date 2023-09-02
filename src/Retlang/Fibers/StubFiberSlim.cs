using System;
using System.Collections.Generic;
using System.Linq;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// StubFiber does not use a backing thread or a thread pool for execution. Actions are added to pending
    /// lists for execution. These actions can be executed synchronously by the calling thread. This class
    /// is not thread safe and should not be used in production code. 
    /// 
    /// The class is typically used for testing asynchronous code to make it completely synchronous and
    /// deterministic.
    /// </summary>
    public class StubFiber : IFiber
    {
        private readonly Subscriptions _subscriptions = new Subscriptions();
        private readonly List<Action> _pending = new List<Action>();
        private readonly Scheduler _scheduler;

        private bool _root = true;

        /// <summary>
        /// Construct new instance.
        /// </summary>
        public StubFiber()
        {
            _scheduler = new Scheduler(this);
        }

        /// <summary>
        /// No Op
        /// </summary>
        public void Start()
        {}

        /// <summary>
        /// Clears all subscriptions, scheduled, and pending actions.
        /// </summary>
        public void Dispose()
        {
            _scheduler.Dispose();
            _subscriptions.Dispose();
            _pending.Clear();
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            if (_root && ExecutePendingImmediately)
            {
                try
                {
                    _root = false;
                    action();
                    ExecuteAllPendingUntilEmpty();
                }
                finally
                {
                    _root = true;
                }
            }
            else
            {
                _pending.Add(action);
            }
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
        /// Adds a scheduled action to the list. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <returns></returns>
        public IDisposable Schedule(Action action, long firstInMs)
        {
            return _scheduler.Schedule(action, firstInMs);
        }

        /// <summary>
        /// Adds scheduled action to list.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="firstInMs"></param>
        /// <param name="regularInMs"></param>
        /// <returns></returns>
        public IDisposable ScheduleOnInterval(Action action, long firstInMs, long regularInMs)
        {
            return _scheduler.ScheduleOnInterval(action, firstInMs, regularInMs);
        }

        /// <summary>
        /// All pending actions.
        /// </summary>
        public List<Action> Pending
        {
            get { return _pending; }
        }

        /// <summary>
        /// If true events will be executed immediately rather than added to the pending list.
        /// </summary>
        public bool ExecutePendingImmediately { get; set; }

        /// <summary>
        /// Execute all actions in the pending list.  If any of the executed actions enqueue more actions, execute those as well.
        /// </summary>
        public void ExecuteAllPendingUntilEmpty()
        {
            while (_pending.Count > 0)
            {
                var toExecute = _pending[0];
                _pending.RemoveAt(0);
                toExecute();
            }
        }

        /// <summary>
        /// Execute all actions in the pending list.
        /// </summary>
        public void ExecuteAllPending()
        {
            var copy = _pending.ToArray();
            _pending.Clear();
            foreach (var pending in copy)
            {
                pending();
            }
        }
    }
}
