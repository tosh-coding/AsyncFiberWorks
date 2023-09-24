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
        ///<param name="fiber">the target executor to receive the message</param>
        ///<param name="control"></param>
        ///<param name="receive"></param>
        ///<param name="timeoutInMs">For initial snapshot</param>
        ///<param name="registry"></param>
        /// <returns></returns>
        IDisposable PrimedSubscribe(IExecutionContext fiber, Action<SnapshotRequestControlEvent> control, Action<T> receive, int timeoutInMs, ISubscriptionRegistry registry);

        ///<summary>
        /// Subscribes for an initial snapshot and then incremental update.
        ///</summary>
        ///<param name="fiber">the target executor to receive the message</param>
        ///<param name="control"></param>
        ///<param name="receive"></param>
        ///<param name="timeoutInMs"></param>
        IDisposable PrimedSubscribe(IFiber fiber, Action<SnapshotRequestControlEvent> control, Action<T> receive, int timeoutInMs);
    }
}
