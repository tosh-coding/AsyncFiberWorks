using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Used to receive one or more replies.
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public interface IReply<M> : IDisposable
    {
        /// <summary>
        /// Receive a single response. Can be called repeatedly for multiple replies.
        /// </summary>
        /// <param name="result">A message.</param>
        /// <returns>Returns false if the buffer is empty or the object has been disposed.</returns>
        bool TryReceive(out M result);

        /// <summary>
        /// Set up on-receive callbacks. It is a one-time call.
        /// </summary>
        /// <param name="callbackOnReceive">Message receive handler.</param>
        /// <returns>Returns false if it has already been disposed.</returns>
        bool SetCallbackOnReceive(Action callbackOnReceive);
    }
}
