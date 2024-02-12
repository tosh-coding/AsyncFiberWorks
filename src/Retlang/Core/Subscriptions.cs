using Retlang.Channels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Retlang.Core
{
    /// <summary>
    /// Registry for subscriptions. Provides thread safe methods for list of subscriptions.
    /// </summary>
    public class Subscriptions : ISubscriptionRegistry, IDisposable
    {
        private readonly object _lock = new object();
        private volatile bool _running = true;
        private readonly LinkedList<IDisposable> _items = new LinkedList<IDisposable>();

        /// <summary>
        /// Add Disposable. It will be unsubscribed when the fiber is discarded.
        /// </summary>
        /// <param name="toAdd"></param>
        /// <returns>A disposer to unregister the subscription.</returns>
        public IDisposable RegisterSubscription(IDisposable toAdd)
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
        /// Register subscription to be unsubcribed from when the fiber is disposed.
        /// </summary>
        /// <param name="toAdd"></param>
        /// <returns>The caller of DeregisterSubscription and the IDisposable.</returns>
        public IDisposable RegisterSubscriptionAndCreateDisposable(IDisposable toAdd)
        {
            var unsubscriber = new Unsubscriber();
            var disposable = this.RegisterSubscription(unsubscriber);
            unsubscriber.Add(() => disposable.Dispose());

            unsubscriber.Add(() => toAdd.Dispose());
            return unsubscriber;
        }

        /// <summary>
        /// Add Disposable. It will be unsubscribed when the fiber is discarded.
        /// It is destroyed at the last.
        /// </summary>
        /// <param name="toAdd"></param>
        public void RegisterSubscriptionLast(IDisposable toAdd)
        {
            bool added = false;
            lock (_lock)
            {
                if (_running)
                {
                    _items.AddLast(toAdd);
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
