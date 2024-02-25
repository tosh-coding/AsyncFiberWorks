using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// One shot executor. It is executed only once the first time.
    /// </summary>
    public class OneShotExecutor : IExecutor
    {
        private bool _fired = false;

        /// <summary>
        /// Execute only first one.
        /// </summary>
        /// <param name="toExecute"></param>
        public void Execute(List<Action> toExecute)
        {
            if (_fired) return;
            foreach (var action in toExecute)
            {
                _fired = true;
                Execute(action);
                break;
            }
        }

        ///<summary>
        /// Executes a single action. 
        ///</summary>
        ///<param name="toExecute"></param>
        public void Execute(Action toExecute)
        {
            if (!_fired)
            {
                _fired = true;
                toExecute();
            }
        }
    }
}
