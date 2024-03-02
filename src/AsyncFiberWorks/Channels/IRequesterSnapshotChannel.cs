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
        /// <param name="control"></param>
        /// <param name="receive"></param>
        IDisposable PrimedSubscribe(Channel<SnapshotRequestControlEvent> control, Channel<T> receive);
    }
}
