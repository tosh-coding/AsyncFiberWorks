using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class QueueConsumerFilterTests
    {
        [Test]
        public void SingleConsumer()
        {
            var subscriptions = new Subscriptions();
            var one = new PoolFiber();
            var oneConsumed = 0;
            var reset = new AutoResetEvent(false);
            using (subscriptions)
            {
                var channel = new Channel<int>();
                Action<int> onMsg = delegate
                {
                    oneConsumed++;
                    if (oneConsumed == 20)
                    {
                        reset.Set();
                    }
                };
                var subscriptionFiber = subscriptions.BeginSubscription();
                var consumer = new QueueConsumerFilter<int>(one, onMsg);
                var subscriptionChannel = channel.Subscribe(one, consumer.Receive);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                for (var i = 0; i < 20; i++)
                {
                    channel.Publish(i);
                }
                Assert.IsTrue(reset.WaitOne(10000, false));
            }
        }

        [Test]
        public void SingleConsumerWithException()
        {
            var exec = new StubExecutor();
            var oneSubscriptions = new Subscriptions();
            var one = new PoolFiber(new DefaultThreadPool(), exec);
            var reset = new AutoResetEvent(false);
            using (oneSubscriptions)
            {
                var channel = new Channel<int>();
                Action<int> onMsg = delegate(int num)
                {
                    if (num == 0)
                    {
                        throw new Exception();
                    }
                    reset.Set();
                };
                var subscriptionFiber = oneSubscriptions.BeginSubscription();
                var consumer = new QueueConsumerFilter<int>(one, onMsg);
                var subscriptionChannel = channel.Subscribe(one, consumer.Receive);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                channel.Publish(0);
                channel.Publish(1);
                Assert.IsTrue(reset.WaitOne(10000, false));
                Assert.AreEqual(1, exec.failed.Count);
            }
        }

        [Test]
        public void Multiple()
        {
            var queues = new List<IDisposable>();
            var receiveCount = 0;
            var reset = new AutoResetEvent(false);
            var channel = new Channel<int>();

            var messageCount = 100;
            var updateLock = new object();
            for (var i = 0; i < 5; i++)
            {
                Action<int> onReceive = delegate
                                            {
                                                Thread.Sleep(15);
                                                lock (updateLock)
                                                {
                                                    receiveCount++;
                                                    if (receiveCount == messageCount)
                                                    {
                                                        reset.Set();
                                                    }
                                                }
                                            };
                var subscriptions = new Subscriptions();
                var fiber = new PoolFiber();
                queues.Add(subscriptions);
                var subscriptionFiber = subscriptions.BeginSubscription();

                var consumer = new QueueConsumerFilter<int>(fiber, onReceive);
                var subscriptionChannel = channel.Subscribe(fiber, consumer.Receive);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
            }
            for (var i = 0; i < messageCount; i++)
            {
                channel.Publish(i);
            }
            Assert.IsTrue(reset.WaitOne(10000, false));
            queues.ForEach(delegate(IDisposable q) { q.Dispose(); });
        }
    }

    public class StubExecutor : IExecutor
    {
        public List<Exception> failed = new List<Exception>();

        public void Execute(IReadOnlyList<Action> toExecute)
        {
            foreach (var action in toExecute)
            {
                Execute(action);
            }
        }

        public void Execute(Action toExecute)
        {
            try
            {
                toExecute();
            }
            catch (Exception e)
            {
                failed.Add(e);
            }
        }
    }
}
