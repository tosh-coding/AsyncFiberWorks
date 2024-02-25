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
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryReceive(out M result);

        /// <summary>
        /// Set up on-receive callbacks.
        /// Also called if timed out. In that case, TryReceive will fail.
        /// </summary>
        /// <param name="callbackOnReceive"></param>
        /// <returns></returns>
        bool SetCallbackOnReceive(Action callbackOnReceive);
    }
}
