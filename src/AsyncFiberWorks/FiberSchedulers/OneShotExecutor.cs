using System;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.FiberSchedulers
{
    /// <summary>
    /// One shot executor. It is executed only once the first time.
    /// </summary>
    public class OneShotExecutor : IExecutor
    {
        private bool _fired = false;

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
