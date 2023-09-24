using System;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// The interface for replies in SnapshotChannel.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IResponderSnapshotChannel<T>
    {
        /// <summary>
        /// Responds to the request for an initial snapshot.
        /// </summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="reply">returns the snapshot update</param>
        /// <returns></returns>
        IDisposable ReplyToPrimingRequest(IFiberWithFallbackRegistry fiber, Func<T> reply);

        /// <summary>
        /// Responds to the request for an initial snapshot.
        /// </summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="reply">returns the snapshot update</param>
        /// <returns></returns>
        IDisposable ReplyToPrimingRequest(IExecutionContext fiber, Func<T> reply, ISubscriptionRegistry fallbackRegistry);

        /// <summary>
        /// Responds to the request for an initial snapshot.
        /// This subscription cannot be unsubscribed. 
        /// </summary>
        /// <param name="executionContext">the target executor to receive the message</param>
        /// <param name="reply">returns the snapshot update</param>
        void PersistentReplyToPrimingRequest(IExecutionContext executionContext, Func<T> reply);

        /// <summary>
        /// Publish a message to all subscribers. Returns true if any subscribers are registered.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        bool Publish(T msg);
    }
}
