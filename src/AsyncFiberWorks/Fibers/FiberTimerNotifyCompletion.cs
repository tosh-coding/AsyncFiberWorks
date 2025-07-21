using AsyncFiberWorks.Core;
using AsyncFiberWorks.Timers;
using System;
using System.Runtime.CompilerServices;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// An implementation of INotifyCompletion for IExecutionContext.
    /// </summary>
    public struct FiberTimerNotifyCompletion : INotifyCompletion
    {
        private readonly IExecutionContext _fiber;
        private readonly IOneshotTimer _timer;
        private readonly int _milliseconds;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fiber">The context to switch to.</param>
        /// <param name="milliseconds">Waiting time. In milliseconds.</param>
        /// <param name="timer">Timer used to wait for time.</param>
        public FiberTimerNotifyCompletion(IExecutionContext fiber, int milliseconds, IOneshotTimer timer)
        {
            _fiber = fiber;
            _timer = timer;
            _milliseconds = milliseconds;
        }

        /// <summary>
        /// await enabling.
        /// </summary>
        /// <returns></returns>
        public FiberTimerNotifyCompletion GetAwaiter()
        {
            return this;
        }

        /// <summary>
        /// Always false, to have the completion process performed.
        /// </summary>
        public bool IsCompleted { get { return false; } }

        /// <summary>
        /// Called to resume subsequent processing at the end of await.
        /// </summary>
        /// <param name="action"></param>
        public void OnCompleted(Action action)
        {
            _timer.Schedule(_fiber, action, _milliseconds);
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void GetResult()
        {}
    }
}
