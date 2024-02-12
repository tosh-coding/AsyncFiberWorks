using System;

namespace Retlang.Channels
{
    internal class Unsubscriber: IDisposable
    {
        private readonly object _lock = new object();
        private Action<Unsubscriber> _actionUnsubscribe;

        private bool _disposed;

        public Unsubscriber(Action<Unsubscriber> action)
        {
            _actionUnsubscribe = action;
        }

        public void Add(Action<Unsubscriber> action)
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
                action(this);
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
            _actionUnsubscribe(this);
        }
    }
}
