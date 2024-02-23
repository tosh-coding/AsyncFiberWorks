using System;
using System.Windows;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Fibers;

namespace WpfExample
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private readonly WindowChannels channels = new WindowChannels();
        private readonly IFiber fiber;

        public Window1()
        {
            InitializeComponent();
            fiber = new DispatcherFiber(Dispatcher);

            var unsubscriberList = new Unsubscriber();
            var subscriber = new LastSubscriber<DateTime>(0, fiber, OnTimeUpdate);
            unsubscriberList.AddDisposable(subscriber);
            fiber.BeginSubscriptionAndSetUnsubscriber(unsubscriberList);
            var unsubscriber = channels.TimeUpdate.Subscribe(subscriber);
            unsubscriberList.AddDisposable(unsubscriber);
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