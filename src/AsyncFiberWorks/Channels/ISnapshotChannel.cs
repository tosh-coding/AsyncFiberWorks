using System;
using System.Threading.Tasks;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    ///<summary>
    /// An ISnapshotChannel is a channel that allows for the transmission of an initial snapshot followed by incremental updates.
    /// The class is thread safe.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public interface ISnapshotChannel<T> : IResponderSnapshotChannel<T>, IRequesterSnapshotChannel<T>
    {
    }
}