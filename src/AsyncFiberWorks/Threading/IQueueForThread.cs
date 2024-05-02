using System;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Holds on to actions until the execution context can process them.
    /// </summary>
    public interface IQueueForThread : IThreadWork, IConsumerThread
    {
    }
}
