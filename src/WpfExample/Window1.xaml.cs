using System;
using System.Windows;
using System.Windows.Threading;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace WpfExample
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private readonly WindowChannels channels = new WindowChannels();
        private readonly IAsyncExecutionContext fiber;
        private readonly Subscriptions subscriptions = new Subscriptions();

        public Window1()
        {
            InitializeComponent();
            var adapter = new DispatcherAdapter(Dispatcher, DispatcherPriority.Normal);
            fiber = new PoolFiberSlim(adapter, new DefaultExecutor());

            var subscriptionFiber = subscriptions.BeginSubscription();

            var subscriber = new LastFilter<DateTime>(0, fiber, OnTimeUpdate);
            var subscriptionChannel = channels.TimeUpdate.Subscribe(fiber, subscriber.Receive);
            subscriptionFiber.AppendDisposable(subscriber, subscriptionChannel);

            new UpdateController(channels);
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            channels.StartChannel.Publish(e);
        }

        private void OnTimeUpdate(DateTime time)
        {
            cpuTextBox.Text = time.ToString();
        }
    }
}