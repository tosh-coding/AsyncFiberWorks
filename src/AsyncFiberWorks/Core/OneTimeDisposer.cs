using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Wrap a discard process that is not IDisposable.
    /// </summary>
    public class OneTimeDisposer : IDisposable
    {
        private readonly object _lockObj = new object();
        private bool _isDisposed;
        private Action _disposable;

        /// <summary>
        /// Set a discard process.
        /// </summary>
        /// <param name="disposable">The process to be registered.</param>
        public OneTimeDisposer(Action disposable)
        {
            _disposable = disposable;
        }

        /// <summary>
        /// Perform a discard operation. Even if it is called multiple times,
        /// the registration process is executed only once.
        /// </summary>
        public void Dispose()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
            }
            _disposable();
        }
    }
}
