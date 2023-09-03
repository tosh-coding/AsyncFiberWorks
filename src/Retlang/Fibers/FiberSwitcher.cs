namespace Retlang.Fibers
{
    /// <summary>
    /// Context switcher for Fibers.
    /// </summary>
    public static class FiberSwitcher
    {
        /// <summary>
        /// Switch the current context to the specified one.
        /// </summary>
        /// <param name="fiber"></param>
        /// <returns></returns>
        public static FiberSlimNotifyCompletion SwitchTo(this IFiberSlim fiber)
        {
            return new FiberSlimNotifyCompletion(fiber);
        }

        /// <summary>
        /// Switch the current context to the specified one.
        /// </summary>
        /// <param name="fiber"></param>
        /// <returns></returns>
        public static FiberNotifyCompletion SwitchTo(this IFiber fiber)
        {
            return new FiberNotifyCompletion(fiber);
        }
    }
}
