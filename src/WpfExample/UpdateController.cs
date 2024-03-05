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
            var subscriptionFiber = threadFiber.BeginSubscription();
            var subscriber = new ChannelSubscription<RoutedEventArgs>(threadFiber, OnStart);
            var subscriptionChannel = channels.StartChannel.Subscribe(subscriber.ReceiveOnProducerThread);
            subscriptionFiber.AppendDisposable(subscriptionChannel);
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
                var subscriptionFiber = fiber.BeginSubscription();
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