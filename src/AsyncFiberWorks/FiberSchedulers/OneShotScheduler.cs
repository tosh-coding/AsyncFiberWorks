using System;

namespace AsyncFiberWorks.FiberSchedulers
{
    /// <summary>
    /// One shot scheduler. It is executed only once the first time.
    /// </summary>
    public class OneShotScheduler
    {
        private bool _fired = false;

        ///<summary>
        /// Executes a single action. 
        ///</summary>
        ///<param name="toExecute"></param>
        public void Schedule(Action toExecute)
        {
            if (!_fired)
            {
                _fired = true;
                toExecute();
            }
        }
    }
}
