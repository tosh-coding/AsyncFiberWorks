using System;

namespace Retlang.Core
{
    /// <summary>
    /// Allows for the registration and deregistration of timers.
    /// </summary>
    public interface ISchedulerRegistry
    {
        /// <summary>
        /// Register a timer. So that it stops together when the fiber is disposed.
        /// </summary>
        /// <param name="timer"></param>
        void RegisterSchedule(IDisposable timer);

        /// <summary>
        /// Deregister a timer.
        /// </summary>
        /// <param name="timer"></param>
        void DeregisterSchedule(IDisposable timer);
    }
}
