﻿using System;
using System.ComponentModel;
using System.Windows;
using AsyncFiberWorks.Channels;

namespace WpfExample
{
    public class WindowChannels
    {
        public readonly IChannel<DateTime> TimeUpdate = new Channel<DateTime>();
        public readonly IChannel<RoutedEventArgs> StartChannel = new Channel<RoutedEventArgs>();
        public readonly IChannel<CancelEventArgs> OnWindowClosing = new Channel<CancelEventArgs>();
    }
}