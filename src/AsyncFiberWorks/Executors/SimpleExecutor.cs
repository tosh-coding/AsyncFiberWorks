using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Executors
{
    /// <summary>
    /// Just simply execute an action.
    /// </summary>
    public class SimpleExecutor : IExecutor
    {
        /// <summary>
        /// Singleton instance.
        /// SimpleExecutorSingle has no members, so it can be shared.
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
    }
}
