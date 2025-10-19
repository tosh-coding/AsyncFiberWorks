using System.Threading;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// A collection of useful FiberAndHandlerPairList operations.
    /// </summary>
    public static class SequentialHandlerExtensions
    {
        /// <summary>
        /// Creates a registered waiter in the handler list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handlerList"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static SequentialHandlerWaiter<T> CreateWaiter<T>(this FiberAndHandlerPairList<T> handlerList, CancellationToken cancellationToken = default)
        {
            var waiter = new SequentialHandlerWaiter<T>(cancellationToken);
            var unregister = handlerList.Add(waiter.Handler);
            waiter.SetDisposable(unregister);
            return waiter;
        }
    }
}
