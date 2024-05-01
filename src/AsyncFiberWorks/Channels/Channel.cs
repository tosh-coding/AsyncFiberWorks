using AsyncFiberWorks.Core;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Default Channel Implementation. Published messages are forwarded to all subscribers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Channel<T> : IChannel<T>
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
            return this._channel.AddHandler((msg) => executionContext.Enqueue(() => receive(msg)));
        }

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(IAsyncExecutionContext executionContext, Func<T, Task<Action>> receive)
        {
            return this._channel.AddHandler((msg) =>
            {
                executionContext.Enqueue(() => receive.Invoke(msg));
            });
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
