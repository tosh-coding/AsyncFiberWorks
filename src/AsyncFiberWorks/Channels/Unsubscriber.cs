using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Disposables.
    /// </summary>
    public class Unsubscriber: IDisposable
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
        /// <param name="disposingAction">A disposable.</param>
        public void Add(Action disposingAction)
        {
            bool added = false;
            lock (_lock)
            {
                if (!_disposed)
                {
                    _actionUnsubscribe += disposingAction;
                    added = true;
                }
            }
            if (!added)
            {
                disposingAction();
            }
        }

        /// <summary>
        /// Add a disposable.
        /// </summary>
        /// <param name="disposable">A disposable.</param>
        public void AddDisposable(IDisposable disposable)
        {
            this.Add(() => disposable.Dispose());
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
