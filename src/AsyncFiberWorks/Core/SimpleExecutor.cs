using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Just simply execute an action.
    /// </summary>
    public class SimpleExecutor : IActionExecutor
    {
        /// <summary>
        /// Singleton instance.
        /// SimpleExecutor has no members, so it can be shared.
        /// </summary>
        public static readonly SimpleExecutor Instance = new SimpleExecutor();

        ///<summary>
        /// Executes a single action. 
        ///</summary>
        ///<param name="toExecute"></param>
        public void Execute(Action toExecute)
        {
            toExecute();
        }

        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="e">Fiber pause operation interface.</param>
        /// <param name="action">Action. Support pause.</param>
        public void Execute(IFiberExecutionEventArgs e, Action<IFiberExecutionEventArgs> action)
        {
            action(e);
        }
    }
}
