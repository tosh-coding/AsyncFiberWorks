using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Delay generator by IOneshotTimerFactory. 
    /// </summary>
    public class OneshotTimerDelayFactory : IHighResDelayFactory
    {
        private readonly IOneshotTimerFactory _timerFactory;

        /// <summary>
        /// Create a delay factory.
        /// </summary>
        /// <param name="timerFactory"></param>
        public OneshotTimerDelayFactory(IOneshotTimerFactory timerFactory)
        {
            _timerFactory = timerFactory;
        }

        /// <summary>
        /// Generate a wait time task.
        /// </summary>
        /// <param name="millisecondsDelay">Wait time.</param>
        /// <returns>A task that is completed after a specified amount of time.</returns>
        public Task Delay(long millisecondsDelay)
        {
            var tcs = new TaskCompletionSource<int>();
            var disposer = new Unsubscriber();
            var timer = _timerFactory.Schedule(() =>
            {
                tcs.SetResult(0);
            }, millisecondsDelay);
            disposer.AppendDisposable(timer);
            return tcs.Task;
        }
    }
}
