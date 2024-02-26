using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Channels
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
        /// <param name="onRequest"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Only one responder can be handled within a single channel.</exception>
        IDisposable ReplyToPrimingRequest(Action<IRequest<object, T>> onRequest);

        /// <summary>
        /// Publish a message to all subscribers. Returns true if any subscribers are registered.
        /// </summary>
        /// <param name="msg"></param>
        void Publish(T msg);
    }
}
