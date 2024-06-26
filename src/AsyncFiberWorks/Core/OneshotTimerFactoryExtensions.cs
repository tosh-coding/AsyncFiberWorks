using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Delay generator by IOneshotTimerFactory. 
    /// </summary>
    public static class OneshotTimerFactoryExtensions
    {
        /// <summary>
        /// Generate a wait time task.
        /// </summary>
        /// <param name="timerFactory">A timer.</param>
        /// <param name="millisecondsDelay">Wait time.</param>
        /// <returns>A task that is completed after a specified amount of time.</returns>
        public static Task Delay(this IOneshotTimerFactory timerFactory, long millisecondsDelay)
        {
            var tcs = new TaskCompletionSource<int>();
            var disposer = new Unsubscriber();
            var timer = timerFactory.Schedule(() =>
            {
                tcs.SetResult(0);
            }, millisecondsDelay);
            disposer.AppendDisposable(timer);
            return tcs.Task;
        }
    }
}
