using System;
using System.Collections.Generic;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Channel subscription methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISubscriber<T>
    {
        /// <summary>
        /// Subscribes an action to be executed for every action posted to the channel. Action should be thread safe. 
        /// Action may be invoked on multiple threads.
        /// </summary>
        /// <param name="receiveOnProducerThread">A message receive process that is performed on the producer/publisher thread. Probably just transfer it to another fiber.</param>
        /// <returns></returns>
        IDisposable SubscribeOnProducerThreads(Action<T> receiveOnProducerThread);
    }
}
