using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    ///<summary>
    /// Default Channel Implementation. Methods are thread safe.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class Channel<T> : IChannel<T>
    {
        private readonly MessageHandlerList<T> _channel = new MessageHandlerList<T>();

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="messageReceiver">Subscriber.</param>
        /// <returns></returns>
        public IDisposableSubscriptionRegistry Subscribe(IMessageReceiver<T> messageReceiver)
        {
            return this._channel.AddHandler(messageReceiver.ReceiveOnProducerThread);
        }

        /// <summary>
        /// <see cref="IPublisher{T}.Publish(T)"/>
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Publish(T msg)
        {
            return _channel.Publish(msg);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _channel.Count; } }
    }
}
