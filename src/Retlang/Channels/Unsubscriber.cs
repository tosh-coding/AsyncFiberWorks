using System;

namespace Retlang.Channels
{
    internal class Unsubscriber: IDisposable
    {
        private readonly Action<Unsubscriber> _actionUnsubscribe;

        public Unsubscriber(Action<Unsubscriber> actionUnsubscribe)
        {
            _actionUnsubscribe = actionUnsubscribe;
        }

        public void Dispose()
        {
            _actionUnsubscribe(this);
        }
    }
}
