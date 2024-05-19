using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Generators for various timers.
    /// </summary>
    public interface ITimerFactory : IOneshotTimerFactory, IIntervalTimerFactory
    {
    }
}
