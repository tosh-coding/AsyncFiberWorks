using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Default executor.
    /// </summary>
    public class DefaultExecutorSingle : IExecutorSingle
    {
        private bool _running = true;

        ///<summary>
        /// Executes a single action.
        ///</summary>
        ///<param name="toExecute"></param>
        public void Execute(Action toExecute)
        {
            if (_running)
            {
                toExecute();
            }
        }

        /// <summary>
        /// When disabled, actions will be ignored by executor. The executor is typically disabled at shutdown
        /// to prevent any pending actions from being executed.
        /// </summary>
        public bool IsEnabled
        {
            get { return _running; }
            set { _running = value; }
        }
    }
}