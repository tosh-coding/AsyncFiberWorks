using System;
using System.Windows;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Fibers;

namespace WpfExample
{
    public class UpdateController
    {
        private readonly IFiber fiber;
        private IDisposable timer;
        private readonly WindowChannels channels;

        public UpdateController(WindowChannels winChannels)
        {
            channels = winChannels;
            var threadFiber = new ThreadFiber();
            var subscriber = new ChannelSubscription<RoutedEventArgs>(threadFiber, OnStart);
            threadFiber.BeginSubscriptionAndSetUnsubscriber(subscriber);
            channels.StartChannel.Subscribe(subscriber);
            threadFiber.Start();
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
                var subscriberSchedule = fiber.ScheduleOnInterval(OnTimer, 1000, 1000);
                timer = subscriberSchedule;
                fiber.BeginSubscriptionAndSetUnsubscriber(subscriberSchedule);
            }
        }

        private void OnTimer()
        {
            channels.TimeUpdate.Publish(DateTime.Now);
        }
    }
}