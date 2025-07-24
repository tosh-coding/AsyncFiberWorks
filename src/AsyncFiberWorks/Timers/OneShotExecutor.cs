using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Timers
{
    /// <summary>
    /// It is executed only once the first time.
    /// </summary>
    public class OneShotExecutor : IExecutor
    {
        private bool _fired = false;

        ///<summary>
        /// Executes a single action.
        ///</summary>
        ///<param name="action"></param>
        public void Execute(Action action)
        {
            if (!_fired)
            {
                _fired = true;
                action();
            }
        }
    }
}
