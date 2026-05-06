using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Handles exceptions raised while executing queued work.
    /// </summary>
    public interface IActionExceptionHandler
    {
        /// <summary>
        /// Called when an exception occurs.
        /// </summary>
        /// <param name="exception"></param>
        void Handle(Exception exception);
    }
}
