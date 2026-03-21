using System;
using System.Collections.Generic;
using System.Threading;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Disposables.
    /// </summary>
    public class CompositeDisposable: IDisposable
    {
        private readonly object _lockObj = new object();
        private readonly List<IDisposable> _disposableList;

        private bool _isDisposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CompositeDisposable(int capacity = 4)
        {
            _disposableList = new List<IDisposable>(capacity);
        }

        /// <summary>
        /// Append a disposable. It will be destroyed in tandem.
        /// </summary>
        /// <param name="disposable">A disposable.</param>
        public void AppendDisposable(IDisposable disposable)
        {
            bool added = false;
            lock (_lockObj)
            {
                if (!_isDisposed)
                {
                    _disposableList.Add(disposable);
                    added = true;
                }
            }
            if (!added)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Append disposables. They will be destroyed in tandem.
        /// </summary>
        /// <param name="disposableList">Disposables.</param>
        public void AppendDisposable(IEnumerable<IDisposable> disposableList)
        {
            bool added = false;
            lock (_lockObj)
            {
                if (!_isDisposed)
                {
                    _disposableList.AddRange(disposableList);
                    added = true;
                }
            }
            if (!added)
            {
                foreach (var disposable in disposableList)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Append disposables. They will be destroyed in tandem.
        /// </summary>
        /// <param name="disposableList">Disposables.</param>
        public void AppendDisposable(params IDisposable[] disposableList)
        {
            bool added = false;
            lock (_lockObj)
            {
                if (!_isDisposed)
                {
                    _disposableList.AddRange(disposableList);
                    added = true;
                }
            }
            if (!added)
            {
                foreach (var disposable in disposableList)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Dispose of all registered disposable.
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
            foreach (var d in _disposableList)
            {
                d?.Dispose();
            }
            _disposableList.Clear();
        }
    }
}
