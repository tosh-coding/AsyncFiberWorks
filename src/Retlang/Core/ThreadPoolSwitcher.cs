namespace Retlang.Core
{
    public static class ThreadPoolSwitcher
    {
        public static ThreadPoolNotifyCompletion SwitchTo(this IThreadPool threadPool)
        {
            return new ThreadPoolNotifyCompletion(threadPool);
        }
    }
}
