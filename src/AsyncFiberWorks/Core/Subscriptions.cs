using System;
using System.Collections.Generic;
using System.Linq;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Registry for subscriptions. Provides thread safe methods for list of subscriptions.
    /// </summary>
    public class Subscriptions : ISubscriptionRegistry, IDisposable, ISubscriptionRegistryViewing
    {
        private readonly object _lock = new object();
        private volatile bool _running = true;
        private readonly LinkedList<IDisposable> _items = new LinkedList<IDisposable>();

        /// <summary>
        /// Register subscription to be unsubcribed from when the fiber is disposed.
        /// </summary>
        /// <param name="toAdd"></param>
        /// <returns>A disposer to unregister the subscription.</returns>
        private IDisposable RegisterSubscription(IDisposable toAdd)
        {
            bool added = false;
            lock (_lock)
            {
                if (_running)
                {
                    _items.AddFirst(toAdd);
                    added = true;
                }
            }
            if (added)
            {
                var unsubscriber = new Unsubscriber(() =>
                {
                    this.DeregisterSubscription(toAdd);
                });
                return unsubscriber;
            }
            else
            {
                toAdd.Dispose();
                return new Unsubscriber();
            }
        }

        /// <summary>
        /// Begin subscription.
        /// </summary>
        /// <returns>Unsubscribers. It is also discarded when the subscription subject is terminated.</returns>
        public Unsubscriber BeginSubscription()
        {
            var unsubscriber = new Unsubscriber();
            var unregister = this.RegisterSubscription(unsubscriber);
            unsubscriber.AppendDisposable(unregister);
            return unsubscriber;
        }

        /// <summary>
        /// Remove Disposable.
        /// </summary>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        private bool DeregisterSubscription(IDisposable toRemove)
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
