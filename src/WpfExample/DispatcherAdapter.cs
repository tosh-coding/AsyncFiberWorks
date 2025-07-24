using AsyncFiberWorks.Core;
using System;
using System.Threading;
using System.Windows.Threading;

namespace WpfExample
{
    internal class DispatcherAdapter : IThreadPool
    {
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherPriority _priority;

        public DispatcherAdapter(Dispatcher dispatcher, DispatcherPriority priority)
        {
            _dispatcher = dispatcher;
            _priority = priority;
        }

        public void Queue(WaitCallback callback)
        {
            Action action = () => callback(null);
            _dispatcher.BeginInvoke(action, _priority);
        }
    }
}