using AsyncFiberWorks.Core;
using System;
using System.ComponentModel;
using System.Threading;

namespace WpfExample
{
    internal class FormAdapter : IThreadPool
    {
        private readonly ISynchronizeInvoke _invoker;

        public FormAdapter(ISynchronizeInvoke invoker)
        {
            _invoker = invoker;
        }

        public void Queue(WaitCallback callback, object state)
        {
            Action action = () => callback(state);
            _invoker.BeginInvoke(action, null);
        }
    }
}