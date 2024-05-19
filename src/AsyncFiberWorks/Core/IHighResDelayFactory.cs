using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Generate a wait time task.
    /// </summary>
    public interface IHighResDelayFactory
    {
        /// <summary>
        /// Generate a wait time task.
        /// </summary>
        /// <param name="millisecondsDelay">Wait time.</param>
        /// <returns>A task that is completed after a specified amount of time.</returns>
        Task Delay(long millisecondsDelay);
    }
}
