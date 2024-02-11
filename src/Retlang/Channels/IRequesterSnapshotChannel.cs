using System;
using Retlang.Core;
using Retlang.Fibers;

namespace Retlang.Channels
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
        ///<param name="requester"></param>
        void OnPrimedSubscribe(SnapshotRequest<T> requester);
    }
}
