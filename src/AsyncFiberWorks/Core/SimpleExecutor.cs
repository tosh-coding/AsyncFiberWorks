using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Just simply execute.
    /// </summary>
    public class SimpleExecutor : IExecutor
    {
        public static readonly SimpleExecutor Instance = new SimpleExecutor();

        /// <summary>
        /// Executes all actions.
        /// </summary>
        /// <param name="toExecute"></param>
        public void Execute(IReadOnlyList<Action> toExecute)
        {
            foreach (var action in toExecute)
            {
                Execute(action);
            }
        }

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
