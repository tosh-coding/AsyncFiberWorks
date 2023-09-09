using System;

namespace Retlang.Core
{
    /// <summary>
    /// A fiber for executing asynchronous actions.
    /// </summary>
    public interface IExecutionContext
    {
        /// <summary>
        /// Enqueue action to the fiber for execution.
        /// </summary>
        /// <param name="action"></param>
        void Enqueue(Action action);
    }
}