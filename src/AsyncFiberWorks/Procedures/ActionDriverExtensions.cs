using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// ActionDriver extension.
    /// </summary>
    public static class ActionDriverExtensions
    {
        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="task">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public static IDisposable SubscribeAndReceiveAsTask(this IActionDriverSubscriber driver, Func<Task> task)
        {
            return driver.Subscribe((e) =>
            {
                e.PauseWhileRunning(task);
            });
        }
    }
}
