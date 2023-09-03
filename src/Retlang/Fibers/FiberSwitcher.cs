namespace Retlang.Fibers
{
    public static class FiberSwitcher
    {
        public static FiberSlimNotifyCompletion SwitchTo(this IFiberSlim fiber)
        {
            return new FiberSlimNotifyCompletion(fiber);
        }

        public static FiberNotifyCompletion SwitchTo(this IFiber fiber)
        {
            return new FiberNotifyCompletion(fiber);
        }
    }
}
