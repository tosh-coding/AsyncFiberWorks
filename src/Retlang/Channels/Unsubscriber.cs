using System;

namespace Retlang.Channels
{
    internal class Unsubscriber: IDisposable
    {
        private readonly object _lock = new object();
        private readonly Action<Unsubscriber> _actionUnsubscribe;

        private bool _disposed;

        public Unsubscriber(Action<Unsubscriber> actionUnsubscribe)
        {
            _actionUnsubscribe = actionUnsubscribe;
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
