using System;
using System.Windows;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.FiberSchedulers;

namespace WpfExample
{
    public class UpdateController
    {
        private readonly IFiber fiber;
        private readonly Subscriptions subscriptions;
        private IDisposable timer;
        private readonly WindowChannels channels;

        public UpdateController(WindowChannels winChannels)
        {
            channels = winChannels;
            subscriptions = new Subscriptions();
            var threadFiber = new ThreadFiber();
            var subscriptionFiber = subscriptions.BeginSubscription();
            var subscriptionChannel = channels.StartChannel.Subscribe(threadFiber, OnStart);
            subscriptionFiber.AppendDisposable(subscriptionChannel);
            fiber = threadFiber;
        }

        private void OnStart(RoutedEventArgs msg)
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            else
            {
                var subscriptionFiber = subscriptions.BeginSubscription();
                var timerDisposable = fiber.ScheduleOnInterval(OnTimer, 1000, 1000);
                subscriptionFiber.AppendDisposable(timerDisposable);
                timer = subscriptionFiber;
            }
        }

        private void OnTimer()
        {
            channels.TimeUpdate.Publish(DateTime.Now);
        }
    }
}