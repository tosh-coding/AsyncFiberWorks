using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes actions and ignores all thrown exceptions.
    /// </summary>
    public class IgnoreExceptionExecutor : IActionExecutor
    {
        /// <summary>
        /// Singleton instance.
        /// IgnoreExceptionExecutor has no members, so it can be shared.
        /// </summary>
        public static readonly IgnoreExceptionExecutor Instance = new IgnoreExceptionExecutor();

        ///<summary>
        /// Executes a single action. 
        ///</summary>
        ///<param name="toExecute"></param>
        public void Execute(Action toExecute)
        {
            try
            {
                toExecute();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="e">Fiber pause operation interface.</param>
        /// <param name="action">Action. Support pause.</param>
        public void Execute(IFiberExecutionEventArgs e, Action<IFiberExecutionEventArgs> action)
        {
            try
            {
                action(e);
            }
            catch (Exception)
            {
            }
        }
    }
}
