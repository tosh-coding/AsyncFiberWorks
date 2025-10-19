using System.Threading;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// A collection of useful FiberAndTaskPairList operations.
    /// </summary>
    public static class SequentialTaskExtensions
    {
        /// <summary>
        /// Creates a registered waiter in the task list.
        /// </summary>
        /// <param name="taskList"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The created waiter is configured to be unregistered from the task list when it is Disposed.</returns>
        public static SequentialTaskWaiter CreateWaiter(this FiberAndTaskPairList taskList, CancellationToken cancellationToken = default)
        {
            var waiter = new SequentialTaskWaiter(cancellationToken);
            var unregister = taskList.Add(waiter.ExecuteTask);
            waiter.SetDisposable(unregister);
            return waiter;
        }
    }
}
