using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfExample
{
    public class UpdateController
    {
        private readonly IFiber _fiber;
        private readonly Subscriptions _subscriptions;
        private readonly WindowChannels _channels;
        private bool _timerIsValid = false;
        private CancellationTokenSource _timerCancellation;

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
            if (_timerIsValid)
            {
                _timerCancellation.Cancel();
                _timerCancellation.Dispose();
                _timerIsValid = false;
            }
            else
            {
                var subscriptionFiber = _subscriptions.BeginSubscription();
                _timerCancellation = new CancellationTokenSource();
                var token = _timerCancellation.Token;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000, token);
                    while (!token.IsCancellationRequested)
                    {
                        _fiber.Enqueue(OnTimer);
                        await Task.Delay(1000, token);
                    }
                });
                _timerIsValid = true;
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