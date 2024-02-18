using System;

namespace AsyncFiberWorks.Channels
{
    public class Unsubscriber: IDisposable
    {
        private readonly object _lock = new object();
        private Action _actionUnsubscribe;

        private bool _disposed;

        public Unsubscriber()
        {
            _actionUnsubscribe = null;
        }

        public Unsubscriber(Action action)
        {
            _actionUnsubscribe = action;
        }

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
            _actionUnsubscribe();
        }
    }
}
