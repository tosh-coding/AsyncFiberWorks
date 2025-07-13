using System;
using System.ComponentModel;
using System.Windows;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.FiberSchedulers;
using AsyncFiberWorks.Threading;

namespace WpfExample
{
    public class UpdateController
    {
        private readonly IFiber _fiber;
        private readonly Subscriptions _subscriptions;
        private readonly WindowChannels _channels;
        private IDisposable _timer;

        public UpdateController(WindowChannels winChannels)
        {
            _channels = winChannels;
            _subscriptions = new Subscriptions();

            var disposables = _subscriptions.BeginSubscription();

            var fiber = AnotherThreadPool.Instance.CreateFiber();
            _fiber = fiber;

            var subscriptionStartChannel = _channels.StartChannel.Subscribe(fiber, OnStart);
            disposables.AppendDisposable(subscriptionStartChannel);

            var subscriptionOnWindowClosing = _channels.OnWindowClosing.Subscribe(fiber, OnWindowClosing);
            disposables.AppendDisposable(subscriptionOnWindowClosing);
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

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _subscriptions.Dispose();
        }
    }
}