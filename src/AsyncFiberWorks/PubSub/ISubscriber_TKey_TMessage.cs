using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.PubSub
{
    /// <summary>
    /// Channel subscription interface.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public interface ISubscriber<TKey, TMessage>
    {
        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="key">Key for specifying a channel.</param>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(TKey key, IExecutionContext executionContext, Action<TMessage> receive);

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="key">Key for specifying a channel.</param>
        /// <param name="executionContext">The execution context of the message receive handler.</param>
        /// <param name="receive">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        IDisposable Subscribe(TKey key, IFiber executionContext, Action<IFiberExecutionEventArgs, TMessage> receive);
    }
}
