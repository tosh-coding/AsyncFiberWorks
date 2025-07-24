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
        /// </summary>
        /// <param name="fiber">The context to switch to.</param>
        /// <returns></returns>
        public static FiberNotifyCompletion SwitchTo(this IExecutionContext fiber)
        {
            return new FiberNotifyCompletion(fiber);
        }
    }
}
