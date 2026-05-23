using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.PubSub
{
    /// <summary>
    /// A channel provides a conduit for messages. It provides methods for publishing and subscribing to messages. 
    /// The class is thread safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Channel<T> : ISubscriber<T>, IPublisher<T>
    {
        private readonly MessageHandlerList<T> _channel = new MessageHandlerList<T>();

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(IExecutionContext executionContext, Action<T> receive)
        {
            Action<object> cachedAction = (s) => receive((T)s);
            return this._channel.AddHandler((msg) => executionContext.Enqueue(cachedAction, msg));
        }

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(IFiber executionContext, Action<IFiberExecutionEventArgs, T> receive)
        {
            Action<IFiberExecutionEventArgs, object> cachedAction = (e, msg) =>
            {
                receive(e, (T)msg);
            };
            return this._channel.AddHandler(
                (msg) => executionContext.Enqueue(cachedAction, msg));
        }

        /// <summary>
        /// <see cref="IPublisher{T}.Publish(T)"/>
        /// </summary>
        /// <param name="msg">A message.</param>
        public void Publish(T msg)
        {
            _channel.Publish(msg);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _channel.Count; } }
    }
}
