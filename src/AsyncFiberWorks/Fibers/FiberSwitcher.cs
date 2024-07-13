using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Context switcher for Fibers.
    /// </summary>
    public static class FiberSwitcher
    {
        /// <summary>
        /// Switch the current context to the specified one.
        /// It is also a flush function that waits for all queued actions to be executed.
        /// </summary>
        /// <param name="fiber">The context to switch to.</param>
        /// <returns></returns>
        public static FiberNotifyCompletion SwitchTo(this IExecutionContext fiber)
        {
            return new FiberNotifyCompletion(fiber);
        }
        /// <summary>
        /// Switch the current context to the specified one.
        /// It is also a flush function that waits for all queued actions to be executed.
        /// </summary>
        /// <param name="fiber">The context to switch to.</param>
        /// <param name="milliseconds">Waiting time. In milliseconds.</param>
        /// <param name="timer">Timer used to wait for time.</param>
        /// <returns></returns>
        public static FiberTimerNotifyCompletion DelayedSwitchTo(this IExecutionContext fiber, int milliseconds, IOneshotTimer timer)
        {
            return new FiberTimerNotifyCompletion(fiber, milliseconds, timer);
        }
    }
}
