using Retlang.Fibers;
using System;
using System.Threading;

namespace Retlang.Core
{
    internal sealed class TimerAction : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Action _action;
        private readonly long _firstIntervalInMs;
        private readonly long _intervalInMs;
        private readonly IFiber _fiber;

        private Timer _timer = null;
        private bool _canceled = false;

        public TimerAction(Action action, long firstIntervalInMs, long intervalInMs, IFiber fiber)
        {
            if (firstIntervalInMs < 0)
            {
                firstIntervalInMs = 0;
            }
            if (intervalInMs <= 0)
            {
                intervalInMs = Timeout.Infinite;
            }
            _action = action;
            _firstIntervalInMs = firstIntervalInMs;
            _intervalInMs = intervalInMs;
            _fiber = fiber;
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }
                _timer = new Timer(x => ExecuteOnTimerThread(), null, _firstIntervalInMs, _intervalInMs);
            }
        }

        private void ExecuteOnTimerThread()
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }
            }

            _fiber.Enqueue(ExecuteOnFiberThread);
        }

        public void ExecuteOnFiberThread()
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }
            }

            _action();
            if (_intervalInMs == Timeout.Infinite)
            {
                this.Dispose();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }
                _canceled = true;
            }
            if (_timer != null)
            {
                _timer.Dispose();
            }
            if (_fiber != null)
            {
                _fiber.DeregisterSchedule(this);
            }
        }
    }
}