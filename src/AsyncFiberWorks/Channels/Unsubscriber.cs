using System;
using System.Collections.Generic;
using System.Threading;

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
        private void PrivateAdd(Action disposingAction)
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
        /// Append a disposable. It will be destroyed in tandem.
        /// </summary>
        /// <param name="disposable">A disposable.</param>
        public void AppendDisposable(IDisposable disposable)
        {
            this.PrivateAdd(() => disposable.Dispose());
        }

        /// <summary>
        /// Append disposables. They will be destroyed in tandem.
        /// </summary>
        /// <param name="disposableList">Disposables.</param>
        public void AppendDisposable(IEnumerable<IDisposable> disposableList)
        {
            foreach (var disposable in disposableList)
            {
                this.PrivateAdd(() => disposable.Dispose());
            }
        }

        /// <summary>
        /// Append disposables. They will be destroyed in tandem.
        /// </summary>
        /// <param name="disposableList">Disposables.</param>
        public void AppendDisposable(params IDisposable[] disposableList)
        {
            foreach (var disposable in disposableList)
            {
                this.PrivateAdd(() => disposable.Dispose());
            }
        }

        /// <summary>
        /// Append a cancellation handle. It will be cancelled in tandem.
        /// </summary>
        /// <param name="cancellation">A cancellation handle.</param>
        public void Append(CancellationTokenSource cancellation)
        {
            this.PrivateAdd(() => cancellation.Cancel());
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
