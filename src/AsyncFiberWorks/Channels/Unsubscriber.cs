using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Disposables.
    /// </summary>
    public class Unsubscriber: ISubscriptionRegistry, IDisposableSubscriptionRegistry, IDisposable
    {
        private readonly object _lock = new object();
        private Action _actionUnsubscribe;

        private bool _disposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Unsubscriber()
        {
            _actionUnsubscribe = null;
        }

        /// <summary>
        /// Add one disposable along with the construction.
        /// </summary>
        /// <param name="action"></param>
        public Unsubscriber(Action action)
        {
            _actionUnsubscribe = action;
        }

        /// <summary>
        /// Add a disposable.
        /// </summary>
        /// <param name="action"></param>
        public void Add(Action action)
        {
            bool added = false;
            lock (_lock)
            {
                if (!_disposed)
                {
                    _actionUnsubscribe += action;
                    added = true;
                }
            }
            if (!added)
            {
                action();
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
            Action action = () => unsubscriber.Dispose();
            this.Add(action);
            Action disposable = () => this.Remove(action);
            unsubscriber.Add(disposable);
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

        private bool Remove(Action action)
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _actionUnsubscribe -= action;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Dispose of all registered disposable.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }
            _actionUnsubscribe?.Invoke();
        }
    }
}