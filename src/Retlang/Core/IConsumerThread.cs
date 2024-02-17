using System;
using System.Threading.Tasks;

namespace Retlang.Core
{
    /// <summary>
    /// A thread that consume actions sequentially.
    /// </summary>
    public interface IConsumerThread
    {
        /// <summary>
        /// Start the thread.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the thread.
        /// </summary>
        void Stop();

        /// <summary>
        /// Returns a task waiting for thread termination.
        /// </summary>
        Task Join();
    }
}
