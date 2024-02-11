using System;
using System.Windows;
using Retlang.Channels;
using Retlang.Fibers;

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

            var disposable = channels.TimeUpdate.SubscribeOnProducerThreads(new LastSubscriber<DateTime>(OnTimeUpdate, fiber, 0, null, fiber.FallbackDisposer));
            fiber.FallbackDisposer?.RegisterSubscriptionAndCreateDisposable(disposable);
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