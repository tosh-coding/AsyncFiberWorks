using AsyncFiberWorks.Channels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AsyncFiberWorks.Core
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
        /// Register subscription to be unsubcribed from when the fiber is disposed.
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
        /// Create and register a new Unsubscriber.
        /// It will be disposed when the subscription target ends.
        /// </summary>
        /// <returns>Created unsubscriber.</returns>
        public Unsubscriber BeginSubscription()
        {
            var unsubscriber = new Unsubscriber();
            var disposable = this.RegisterSubscription(unsubscriber);
            unsubscriber.Add(() => disposable.Dispose());
            return unsubscriber;
        }

        /// <summary>
        /// Begin a subscription. Then set its unsubscriber to disposable.
        /// </summary>
        /// <param name="disposable">Disposables that can be reserved for unsubscriptions.</param>
        public void BeginSubscriptionAndSetUnsubscriber(IDisposableSubscriptionRegistry disposable)
        {
            var rootSubscription = this.BeginSubscription();
            rootSubscription.Add(() => disposable.Dispose());

            var branchDisposer = disposable.BeginSubscription();
            branchDisposer.Add(() => rootSubscription.Dispose());
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