using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes an action.
    /// </summary>
    public interface IExecutor
    {
        ///<summary>
        /// Executes a single action.
        ///</summary>
        ///<param name="action"></param>
        void Execute(Action action);
    }
}