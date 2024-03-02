using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Channels
{
    ///<summary>
    /// A SnapshotChannel is a channel that allows for the transmission of an initial snapshot followed by incremental updates.
    /// The class is thread safe.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class SnapshotChannel<T> : ISnapshotChannel<T>
    {
        private readonly Channel<IRequest<Channel<T>, IDisposable>> _requestChannel = new Channel<IRequest<Channel<T>, IDisposable>>();

        ///<summary>
        /// Subscribes for an initial snapshot and then incremental update.
        ///</summary>
        /// <param name="control"></param>
        /// <param name="receive"></param>
        public IDisposable PrimedSubscribe(Channel<SnapshotRequestControlEvent> control, Channel<T> receive)
        {
            var requester = new SnapshotRequest<T>(control, receive);
            requester.StartSubscribe(_requestChannel);
            return requester;
        }

        /// <summary>
        /// Responds to the request for an initial snapshot.
        /// </summary>
        /// <param name="onRequest"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Only one responder can be handled within a single channel.</exception>
        public IDisposable ReplyToPrimingRequest(Action<IRequest<Channel<T>, IDisposable>> onRequest)
        {
            if (_requestChannel.NumSubscribers > 0)
            {
                throw new InvalidOperationException("Only one responder can be handled within a single channel.");
            }
            return _requestChannel.Subscribe(onRequest);
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _requestChannel.NumSubscribers; } }
    }
}
