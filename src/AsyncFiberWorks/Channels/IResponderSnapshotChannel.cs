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
        /// <param name="subscriber"></param>
        void ReplyToPrimingRequest(RequestReplyChannelSubscriber<object, T> subscriber);

        /// <summary>
        /// Publish a message to all subscribers. Returns true if any subscribers are registered.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        bool Publish(T msg);
    }
}
