using AsyncFiberWorks.Core;
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
        /// <param name="timeoutInMs"></param>
        /// <param name="fiberOnReceive">A Fiber on which the on-receive callback runs. If null it runs on .NET ThreadPool.</param>
        /// <param name="callbackOnReceive"></param>
        /// <param name="argumentOfCallback"></param>
        /// <returns></returns>
        bool SetCallbackOnReceive(int timeoutInMs, IExecutionContext fiberOnReceive, Action<object> callbackOnReceive, object argumentOfCallback = null);
    }
}
