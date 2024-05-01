using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes pending action.
    /// </summary>
    public interface IExecutorSingle
    {
        ///<summary>
        /// Executes a single action.
        ///</summary>
        ///<param name="action"></param>
        void Execute(Action action);
    }
}