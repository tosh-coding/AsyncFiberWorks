using System;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// The interface for requests of SnapshotChannel.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRequesterSnapshotChannel<T>
    {
        ///<summary>
        /// Subscribes for an initial snapshot and then incremental update.
        ///</summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="control"></param>
        /// <param name="receive"></param>
        /// <param name="timeoutInMs">For initial snapshot</param>
        IDisposable PrimedSubscribe(ISubscribableFiber fiber, Action<SnapshotRequestControlEvent> control, Action<T> receive, int timeoutInMs);
    }
}
