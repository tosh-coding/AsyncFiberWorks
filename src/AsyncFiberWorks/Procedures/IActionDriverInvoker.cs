using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Action driver invoking interface.
    /// </summary>
    public interface IActionDriverInvoker
    {
        /// <summary>
        /// Invoke all subscribers.
        /// </summary>
        /// <param name="fiber"></param>
        void Invoke(IFiber fiber);
    }
}
