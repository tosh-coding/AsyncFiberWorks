using System;
using AsyncFiberWorks.Core;

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
        IDisposable PrimedSubscribe(IExecutionContext fiber, Action<SnapshotRequestControlEvent> control, Action<T> receive);
    }
}
