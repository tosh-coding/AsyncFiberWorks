using Retlang.Fibers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Retlang.Core
{
    ///<summary>
    /// Enqueues actions on to context after schedule elapses.  
    ///</summary>
    public class Scheduler : IDisposable
    {
        private readonly object _lock = new object();
        private volatile bool _running = true;
        private List<IDisposable> _pending = new List<IDisposable>();

        /// <summary>
        /// Enqueues action on to context after timer elapses.  
        /// </summary>
        /// <param name="timerAction"></param>
        public void Add(IDisposable timerAction)
        {
            bool added = false;
            lock (_lock)
            {
                if (_running)
                {
                    _pending.Add(timerAction);
                    added = true;
                }
            }
            if (!added)
            {
                timerAction.Dispose();
            }
        }

        ///<summary>
        /// Removes a pending scheduled action.
        ///</summary>
        ///<param name="toRemove"></param>
        public void Remove(IDisposable toRemove)
        {
            lock (_lock)
            {
                _pending.Remove(toRemove);
            }
        }

        ///<summary>
        /// Cancels all pending actions
        ///</summary>
        public void Dispose()
        {
            List<IDisposable> old;
            lock (_lock)
            {
                _running = false;
                old = _pending.ToList();
            }
            foreach (var timer in old)
            {
                timer.Dispose();
            }
        }

        /// <summary>
        /// Number of pending timers.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _pending.Count;
                }
            }
        }
    }
}