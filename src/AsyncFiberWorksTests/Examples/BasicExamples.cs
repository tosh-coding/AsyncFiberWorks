using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorksTests.Examples
{
    [TestFixture]
    public class BasicExamples
    {
        [Test]
        public void PubSubWithPool()
        {
            //PoolFiber uses the .NET thread pool by default
            using (var fiber = new PoolFiber())
            {
                var channel = new Channel<string>();

                var reset = new AutoResetEvent(false);
                var subscriptionFiber = fiber.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(fiber, delegate { reset.Set(); });
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                channel.Publish("hello");

                Assert.IsTrue(reset.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithDedicatedThread()
        {
            using (var fiber = new ThreadFiber())
            {
                fiber.Start();
                var channel = new Channel<string>();

                var reset = new AutoResetEvent(false);
                var subscriptionFiber = fiber.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(fiber, delegate { reset.Set(); });
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                channel.Publish("hello");

                Assert.IsTrue(reset.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithDedicatedThreadWithFilter()
        {
            using (var fiber = new ThreadFiber())
            {
                fiber.Start();
                var channel = new Channel<int>();

                var reset = new AutoResetEvent(false);
                Action<int> onMsg = x =>
                {
                    Assert.IsTrue(x % 2 == 0);
                    if (x == 4)
                    {
                        reset.Set();
                    }
                };
                var subscriptionFiber = fiber.BeginSubscription();
                var filters = new List<Filter<int>>();
                filters.Add(x => x % 2 == 0);
                var filter = new MessageFilter<int>(filters, fiber, onMsg);
                var subscriptionChannel = channel.Subscribe(fiber, filter.Receive);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                channel.Publish(1);
                channel.Publish(2);
                channel.Publish(3);
                channel.Publish(4);

                Assert.IsTrue(reset.WaitOne(5000, false));
            }
        }

        [Test]
        public void Batching()
        {
            using (var fiber = new ThreadFiber())
            {
                fiber.Start();
                var counter = new Channel<int>();
                var reset = new ManualResetEvent(false);
                var total = 0;
                Action<IList<int>> cb = delegate(IList<int> batch)
                                            {
                                                total += batch.Count;
                                                if (total == 10)
                                                {
                                                    reset.Set();
                                                }
                                            };

                var subscriptionFiber = fiber.BeginSubscription();
                var subscriber = new BatchFilter<int>(1, fiber, cb);
                var subscriptionChannel = counter.Subscribe(fiber, subscriber.Receive);
                subscriptionFiber.AppendDisposable(subscriber, subscriptionChannel);

                for (var i = 0; i < 10; i++)
                {
                    counter.Publish(i);
                }

                Assert.IsTrue(reset.WaitOne(10000, false));
            }
        }

        [Test]
        public void BatchingWithKey()
        {
            using (var fiber = new ThreadFiber())
            {
                fiber.Start();
                var counter = new Channel<int>();
                var reset = new ManualResetEvent(false);
                Action<IDictionary<String, int>> cb = delegate(IDictionary<String, int> batch)
                {
                    if (batch.ContainsKey("9"))
                    {
                        reset.Set();
                    }
                };

                var disposables = new List<IDisposable>();
                var subscriptionFiber = fiber.BeginSubscription();
                Converter<int, String> keyResolver = x => x.ToString();
                var subscriber = new KeyedBatchFilter<string, int>(keyResolver, 0, fiber, cb);
                disposables.Add(subscriber);
                var subscriptionChannel = counter.Subscribe(fiber, subscriber.Receive);
                disposables.Add(subscriptionChannel);
                subscriptionFiber.AppendDisposable(disposables);

                for (var i = 0; i < 10; i++)
                {
                    counter.Publish(i);
                }

                Assert.IsTrue(reset.WaitOne(10000, false));
            }
        }

        [Test]
        public void RequestReply()
        {
            // Thread for Assert.
            var testThread = new ThreadPoolAdaptorFromQueueForThread();
            var testFiber = new PoolFiber(testThread, new DefaultExecutor());

            using (var fiber = new PoolFiber())
            {
                var channel = new Channel<IRequest<string, string>>();
                var subscriptionFiber = fiber.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(fiber, (req) => req.ReplyTo.Publish("bye"));
                subscriptionFiber.AppendDisposable(subscriptionChannel);

                var disposables = new Unsubscriber();
                var timeoutTimer = testFiber.Schedule(() =>
                {
                    disposables.Dispose();
                    Assert.Fail();
                }, 10000);
                var replyChannel = new Channel<string>();
                var reply = replyChannel.Subscribe(testFiber, (result) =>
                {
                    timeoutTimer.Dispose();
                    Assert.AreEqual("bye", result);
                    testThread.Stop();
                });
                channel.Publish(new RequestReplyChannelRequest<string, string>("hello", replyChannel));
                disposables.AppendDisposable(reply);
                testThread.Run();
            }
        }

        [Test]
        public void ShouldIncreasePoolFiberSubscriberCountByOne()
        {
            var fiber = new PoolFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var subscriptionFiber = fiber.BeginSubscription();
            var subscriptionCHannel = channel.Subscribe(fiber, x => { });
            subscriptionFiber.AppendDisposable(subscriptionCHannel);

            Assert.AreEqual(1, fiber.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            fiber.Dispose();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreaseThreadFiberSubscriberCountByOne()
        {
            var fiber = new ThreadFiber();
            fiber.Start();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var subscriptionFiber = fiber.BeginSubscription();
            var subscriptionChannel = channel.Subscribe(fiber, x => { });
            subscriptionFiber.AppendDisposable(subscriptionChannel);

            Assert.AreEqual(1, fiber.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            fiber.Dispose();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreaseStubFiberSubscriberCountByOne()
        {
            var fiber = new StubFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var subscriptionFiber = fiber.BeginSubscription();
            var subscriptionChannel = channel.Subscribe(fiber, x => { });
            subscriptionFiber.AppendDisposable(subscriptionChannel);

            Assert.AreEqual(1, fiber.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            fiber.Dispose();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void UnsubscriptionShouldRemoveSubscriber()
        {
            var fiber = new PoolFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);

            var subscriptionFiber = fiber.BeginSubscription();
            var subscriptionChannel = channel.Subscribe(fiber, x => { });
            subscriptionFiber.AppendDisposable(subscriptionChannel);

            Assert.AreEqual(1, fiber.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            subscriptionFiber.Dispose();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }
    }
}