using System;
using System.Runtime.CompilerServices;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// A Implementation of INotifyCompletion for IThreadPool.
    /// </summary>
    public struct ThreadPoolNotifyCompletion : INotifyCompletion
    {
        private readonly IThreadPool _threadPool;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="threadPool"></param>
        public ThreadPoolNotifyCompletion(IThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        /// <summary>
        /// await enabling.
        /// </summary>
        /// <returns></returns>
        public ThreadPoolNotifyCompletion GetAwaiter()
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
            _threadPool.Queue((_) => action());
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void GetResult()
        {}
    }
}
