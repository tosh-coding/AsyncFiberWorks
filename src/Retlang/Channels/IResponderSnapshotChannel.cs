using System;
using Retlang.Fibers;

namespace Retlang.Channels
{
    /// <summary>
    /// The interface for replies in SnapshotChannel.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IResponderSnapshotChannel<T>
    {
        ///<summary>
        /// Ressponds to the request for an initial snapshot.
        ///</summary>
        ///<param name="fiber">the target executor to receive the message</param>
        ///<param name="reply">returns the snapshot update</param>
        IDisposable ReplyToPrimingRequest(IFiber fiber, Func<T> reply);

        /// <summary>
        /// Publish a message to all subscribers. Returns true if any subscribers are registered.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        bool Publish(T msg);
    }
}
