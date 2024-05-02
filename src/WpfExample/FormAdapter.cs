using System;
using System.ComponentModel;
using System.Threading;
using AsyncFiberWorks.Threading;

namespace WpfExample
{
    internal class FormAdapter : IThreadPool
    {
        private readonly ISynchronizeInvoke _invoker;

        public FormAdapter(ISynchronizeInvoke invoker)
        {
            _invoker = invoker;
        }

        public void Queue(WaitCallback callback)
        {
            Action action = () => callback(null);
            _invoker.BeginInvoke(action, null);
        }
    }
}