namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Driver invoking interface.
    /// </summary>
    public interface IActionInvoker
    {
        /// <summary>
        /// Invoke all subscribers.
        /// </summary>
        void Invoke();
    }
}
