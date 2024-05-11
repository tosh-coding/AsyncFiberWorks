using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class BusyWaitQueueLatencyTests
    {
        [Test]
        [Explicit]
        public void CompareBusyWaitQueueVsDefaultQueueLatency()
        {
            Func<ThreadFiber> blocking = () => new ThreadFiber();
            Func<ThreadFiber> polling = () => new ThreadFiber(new BusyWaitQueue(100000, 30000));

            for (var i = 0; i < 20; i++)
            {
                Execute(blocking, "Blocking");
                Execute(polling, "Polling");
                Console.WriteLine();
            }
        }

        private static void Execute(Func<ThreadFiber> creator, String name)
        {
            Console.WriteLine(name);

            const int channelCount = 5;
            var msPerTick = 1000.0/Stopwatch.Frequency;

            var channels = new Channel<Msg>[channelCount];

            for (var i = 0; i < channels.Length; i++)
            {
                channels[i] = new Channel<Msg>();
            }

            var fibers = new ThreadFiber[channelCount];
            var subscriptionsList = new Subscriptions[channelCount];
            for (var i = 0; i < fibers.Length; i++)
            {
                fibers[i] = creator();
                subscriptionsList[i] = new Subscriptions();
                var prior = i - 1;
                var isLast = i + 1 == fibers.Length;
                var target = !isLast ? channels[i] : null;

                if (prior >= 0)
                {
                    Action<Msg> cb = delegate(Msg message)
                                         {
                                             if (target != null)
                                             {
                                                 target.Publish(message);
                                             }
                                             else
                                             {
                                                 var now = Stopwatch.GetTimestamp();
                                                 var diff = now - message.Time;
                                                 if (message.Log)
                                                 {
                                                     Console.WriteLine("qTime: " + diff * msPerTick);
                                                 }
                                                 message.Latch.Set();
                                             }
                                         };

                    var fiber = fibers[i];
                    var subscriptions = subscriptionsList[i];
                    var subscriptionFiber = subscriptions.BeginSubscription();
                    var subscriptionChannel = channels[prior].Subscribe(fiber, cb);
                    subscriptionFiber.AppendDisposable(subscriptionChannel);
                }
            }

            for (var i = 0; i < 10000; i++)
            {
                var s = new Msg(false);
                channels[0].Publish(s);
                s.Latch.WaitOne();
            }

            for (var i = 0; i < 5; i++)
            {
                var s = new Msg(true);
                channels[0].Publish(s);
                Thread.Sleep(10);
            }

            foreach (var subscriptions in subscriptionsList)
            {
                subscriptions.Dispose();
            }

            foreach (var fiber in fibers)
            {
                fiber.Dispose();
            }
        }

        private class Msg
        {
            public readonly bool Log;
            public readonly long Time = Stopwatch.GetTimestamp();
            public readonly ManualResetEvent Latch = new ManualResetEvent(false);

            public Msg(bool log)
            {
                Log = log;
            }
        }
    }
}