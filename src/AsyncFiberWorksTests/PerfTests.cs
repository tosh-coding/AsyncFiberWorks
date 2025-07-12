using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Executors;
using AsyncFiberWorks.Threading;
using NUnit.Framework;
using System;
using System.Threading;

namespace AsyncFiberWorksTests
{
    public class PerfExecutor : IHookOfBatch
    {
        public void OnBeforeExecute(int numberOfActions)
        {
        }

        public void OnAfterExecute(int numberOfActions)
        {
            if (numberOfActions < 10000)
            {
                Thread.Sleep(1);
            }
        }
    }

    public struct MsgStruct
    {
        public int count;
    }

    [TestFixture]
    public class PerfTests
    {
        [Test, Explicit]
        public void PointToPointPerfTestWithStructBoundedQueue()
        {
            var queue = new BoundedQueue(new PerfExecutor(), SimpleExecutor.Instance) { MaxDepth = 10000, MaxEnqueueWaitTimeInMs = 1000 };
            PointToPointPerfTestWithStructInternal(queue);
        }

        [Test, Explicit]
        public void PointToPointPerfTestWithStructBusyWaitQueue()
        {
            var queue = new BusyWaitQueue(100000, 30000, new PerfExecutor(), SimpleExecutor.Instance);
            PointToPointPerfTestWithStructInternal(queue);
        }

        private static void PointToPointPerfTestWithStructInternal(IDedicatedConsumerThreadWork queue)
        {
            using (var consumerThread = ConsumerThread.StartNew(queue))
            using (var subscriptions = new Subscriptions())
            {
                var channel = new Channel<MsgStruct>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                Action<MsgStruct> onMsg = delegate(MsgStruct count)
                {
                    if (count.count == max)
                    {
                        reset.Set();
                    }
                };
                var subscriptionFiber = subscriptions.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(consumerThread, onMsg);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                using (new PerfTimer(max))
                {
                    for (var i = 0; i <= max; i++)
                    {
                        channel.Publish(new MsgStruct { count = i });
                    }
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }

        [Test, Explicit]
        public void PointToPointPerfTestWithInt()
        {
            var queue = new BoundedQueue(new PerfExecutor(), SimpleExecutor.Instance) { MaxDepth = 10000, MaxEnqueueWaitTimeInMs = 1000 };
            using (var consumerThread = ConsumerThread.StartNew(queue))
            using (var subscriptions = new Subscriptions())
            {
                var channel = new Channel<int>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                Action<int> onMsg = delegate(int count)
                                        {
                                            if (count == max)
                                            {
                                                reset.Set();
                                            }
                                        };
                var subscriptionFiber = subscriptions.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(consumerThread, onMsg);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                using (new PerfTimer(max))
                {
                    for (var i = 0; i <= max; i++)
                    {
                        channel.Publish(i);
                    }
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }

        [Test, Explicit]
        public void PointToPointPerfTestWithObject()
        {
            var queue = new BoundedQueue(new PerfExecutor(), SimpleExecutor.Instance) { MaxDepth = 100000, MaxEnqueueWaitTimeInMs = 1000 };
            using (var consumerThread = ConsumerThread.StartNew(queue))
            using (var subscriptions = new Subscriptions())
            {
                var channel = new Channel<object>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                var end = new object();
                Action<object> onMsg = delegate(object msg)
                                           {
                                               if (msg == end)
                                               {
                                                   reset.Set();
                                               }
                                           };
                var subscriptionFiber = subscriptions.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(consumerThread, onMsg);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                using (new PerfTimer(max))
                {
                    var msg = new object();
                    for (var i = 0; i <= max; i++)
                    {
                        channel.Publish(msg);
                    }
                    channel.Publish(end);
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }
    }
}