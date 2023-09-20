using System;
using System.Collections.Generic;

namespace Retlang.Core
{
    /// <summary>
    /// Registry for subscriptions. Provides thread safe methods for list of subscriptions.
    /// </summary>
    public class Subscriptions : ISubscriptionRegistry, IDisposable
    {
        private readonly object _lock = new object();
        private volatile bool _running = true;
        private readonly List<IDisposable> _items = new List<IDisposable>();

        /// <summary>
        /// Add Disposable. It will be unsubscribed when the fiber is discarded.
        /// </summary>
        /// <param name="toAdd"></param>
        public void RegisterSubscription(IDisposable toAdd)
        {
            bool added = false;
            lock (_lock)
            {
                if (_running)
                {
                    _items.Add(toAdd);
                    added = true;
                }
            }
            if (!added)
            {
                toAdd.Dispose();
            }
        }

        /// <summary>
        /// Remove Disposable.
        /// </summary>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        public bool DeregisterSubscription(IDisposable toRemove)
        {
            lock (_lock)
            {
                if (!_running)
                {
                    return false;
                }
                return _items.Remove(toRemove);
            }
        }

        /// <summary>
        /// Disposes all disposables registered in list.
        /// </summary>
        public void Dispose()
        {
            IDisposable[] old = null;
            lock (_lock)
            {
                if (!_running)
                {
                    return;
                }

                _running = false;
                if (_items.Count > 0)
                {
                    old = _items.ToArray();
                    _items.Clear();
                }
            }
            if (old != null)
            {
                foreach (var timer in old)
                {
                    timer.Dispose();
                }
            }
        }

        /// <summary>
        /// Number of registered disposables.
        /// </summary>
        public int NumSubscriptions
        {
            get
            {
                lock (_lock)
                {
                    if (!_running)
                    {
                        return 0;
                    }
                    return _items.Count;
                }
            }
        }
    }
}
