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
        /// <param name="fiber"></param>
        /// <returns></returns>
        public static FiberNotifyCompletion SwitchTo(this IExecutionContext fiber)
        {
            return new FiberNotifyCompletion(fiber);
        }
    }
}
