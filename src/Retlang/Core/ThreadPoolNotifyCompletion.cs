using System;
using System.Runtime.CompilerServices;

namespace Retlang.Core
{
    public class ThreadPoolNotifyCompletion : INotifyCompletion
    {
        private readonly IThreadPool _threadPool;

        public ThreadPoolNotifyCompletion(IThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        public ThreadPoolNotifyCompletion GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted { get { return false; } }

        public void OnCompleted(Action action)
        {
            _threadPool.Queue((_) => action());
        }

        public void GetResult()
        {}
    }
}
