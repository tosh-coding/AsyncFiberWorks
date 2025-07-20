using AsyncFiberWorks.Core;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Executors
{
    /// <summary>
    /// Just simply execute an action.
    /// </summary>
    public class SimpleActionExecutor : IActionExecutor
    {
        /// <summary>
        /// Singleton instance.
        /// SimpleExecutorSingle has no members, so it can be shared.
        /// </summary>
        public static readonly SimpleActionExecutor Instance = new SimpleActionExecutor();

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
