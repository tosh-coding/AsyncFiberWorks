using System;
using System.Windows;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.FiberSchedulers;

namespace WpfExample
{
    public class UpdateController
    {
        private readonly IFiber _fiber;
        private readonly Subscriptions _subscriptions;
        private IDisposable _timer;
        private readonly WindowChannels _channels;

        public UpdateController(WindowChannels winChannels)
        {
            _channels = winChannels;
            _subscriptions = new Subscriptions();
            var threadFiber = new ThreadFiber();
            var subscriptionFiber = _subscriptions.BeginSubscription();
            var subscriptionChannel = _channels.StartChannel.Subscribe(threadFiber, OnStart);
            subscriptionFiber.AppendDisposable(subscriptionChannel);
            _fiber = threadFiber;
        }

        private void OnStart(RoutedEventArgs msg)
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
            else
            {
                var subscriptionFiber = _subscriptions.BeginSubscription();
                var timer = new IntervalThreadingTimer();
                timer.ScheduleOnInterval(_fiber, OnTimer, 1000, 1000);
                _timer = timer;
            }
        }

        private void OnTimer()
        {
            _channels.TimeUpdate.Publish(DateTime.Now);
        }
    }
}