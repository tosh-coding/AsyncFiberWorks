using System;

namespace Retlang.Core
{
    /// <summary>
    /// Holds on to actions until the execution context can process them.
    /// This is for ThreadFiber, and the Run method would be blocked.
    /// </summary>
    public interface IQueue
    {
        ///<summary>
        /// Enqueues action for execution context to process.
        ///</summary>
        ///<param name="action"></param>
        void Enqueue(Action action);

        /// <summary>
        /// Start consuming actions.
        /// Does not return from the call until it stops.
        /// </summary>
        void Run();

        /// <summary>
        /// Stop consuming actions.
        /// </summary>
        void Stop();
    }
}
