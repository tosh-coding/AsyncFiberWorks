using System;
using System.ComponentModel;
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
        private readonly IFiber _fiber;
        private readonly Subscriptions _subscriptions = new Subscriptions();
        private readonly IPublisher<RoutedEventArgs> _startChannel;
        private readonly IPublisher<CancelEventArgs> _channelOnWindowClosing;

        public Window1()
        {
            InitializeComponent();
            var adapter = new DispatcherAdapter(Dispatcher, DispatcherPriority.Normal);
            _fiber = new PoolFiber(adapter);

            var disposables = _subscriptions.BeginSubscription();

            var lastFilter = new LastFilter<DateTime>(0, _fiber, OnTimeUpdate);
            disposables.AppendDisposable(lastFilter);

            var subscriberTimeUpdate = ChannelLocator.GetSubscriber<DateTime>();
            var d = subscriberTimeUpdate.Subscribe(_fiber, lastFilter.Receive);
            disposables.AppendDisposable(d);

            Closing += this.OnWindowClosing;

            _startChannel = ChannelLocator.GetPublisher<RoutedEventArgs>();
            _channelOnWindowClosing = ChannelLocator.GetPublisher<CancelEventArgs>();

            new UpdateController();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            _startChannel.Publish(e);
        }

        private void OnTimeUpdate(DateTime time)
        {
            cpuTextBox.Text = time.ToString();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _channelOnWindowClosing.Publish(e);
        }
    }
}