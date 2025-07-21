using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;
using AsyncFiberWorks.Timers;

namespace AsyncFiberWorksTests.Examples
{
    [TestFixture]
    public class BasicExamples
    {
        [Test]
        public void PubSubWithPool()
        {
            //PoolFiber uses the .NET thread pool by default
            using (var subscriptions = new Subscriptions())
            {
                var fiber = new PoolFiber();
                var channel = new Channel<string>();

                var reset = new AutoResetEvent(false);
                var unsubscriber = subscriptions.BeginSubscription();
                var disposableChannel = channel.Subscribe(fiber, (msg) => reset.Set());
                unsubscriber.AppendDisposable(disposableChannel);
                channel.Publish("hello");

                Assert.IsTrue(reset.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithDedicatedThread()
        {
            using (var threadPool = UserThreadPool.StartNew(1))
            using (var subscriptions = new Subscriptions())
            {
                var fiber = new PoolFiber(threadPool);
                var channel = new Channel<string>();

                var reset = new AutoResetEvent(false);
                var unsubscriber = subscriptions.BeginSubscription();
                var disposableChannel = channel.Subscribe(fiber, (msg) => reset.Set());
                unsubscriber.AppendDisposable(disposableChannel);
                channel.Publish("hello");

                Assert.IsTrue(reset.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithAnotherThreadPool()
        {
            using (var subscriptions = new Subscriptions())
            {
                var fiber = AnotherThreadPool.Instance.CreateFiber();
                var channel = new Channel<string>();

                var reset = new AutoResetEvent(false);
                var unsubscriber = subscriptions.BeginSubscription();
                var disposableChannel = channel.Subscribe(fiber, (msg) => reset.Set());
                unsubscriber.AppendDisposable(disposableChannel);
                channel.Publish("hello");

                Assert.IsTrue(reset.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithDedicatedThreadWithFilter()
        {
            using (var threadPool = UserThreadPool.StartNew())
            using (var subscriptions = new Subscriptions())
            {
                var fiber = threadPool.CreateFiber();
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
                var unsubscriber = subscriptions.BeginSubscription();
                var filters = new List<Filter<int>>();
                filters.Add(x => x % 2 == 0);
                var filter = new MessageFilter<int>(filters, fiber, onMsg);
                var disposableChannel = channel.Subscribe(fiber, filter.Receive);
                unsubscriber.AppendDisposable(disposableChannel);
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
            using (var threadPool = UserThreadPool.StartNew())
            using (var subscriptions = new Subscriptions())
            {
                var fiber = threadPool.CreateFiber();
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

                var unsubscriber = subscriptions.BeginSubscription();
                var filter = new BatchFilter<int>(1, fiber, cb);
                var disposableChannel = counter.Subscribe(fiber, filter.Receive);
                unsubscriber.AppendDisposable(filter, disposableChannel);

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
            using (var threadPool = UserThreadPool.StartNew())
            using (var subscriptions = new Subscriptions())
            {
                var fiber = threadPool.CreateFiber();
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
                var unsubscriber = subscriptions.BeginSubscription();
                Converter<int, String> keyResolver = x => x.ToString();
                var filter = new KeyedBatchFilter<string, int>(keyResolver, 0, fiber, cb);
                disposables.Add(filter);
                var disposableChannel = counter.Subscribe(fiber, filter.Receive);
                disposables.Add(disposableChannel);
                unsubscriber.AppendDisposable(disposables);

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
            var testThread = new ThreadPoolAdapter();
            var testFiber = new PoolFiber(testThread);
            var timer = new OneshotThreadingTimer();

            using (var subscriptions = new Subscriptions())
            {
                var fiber = new PoolFiber();
                var channel = new Channel<IRequest<string, string>>();
                var unsubscriber = subscriptions.BeginSubscription();
                var disposableChannel = channel.Subscribe(fiber, (req) => req.ReplyTo.Publish("bye"));
                unsubscriber.AppendDisposable(disposableChannel);

                var disposables = new Unsubscriber();
                var cancellation = new CancellationTokenSource();
                timer.Schedule(testFiber, () =>
                {
                    disposables.Dispose();
                    Assert.Fail();
                }, 10000, cancellation.Token);
                var replyChannel = new Channel<string>();
                var disposableReply = replyChannel.Subscribe(testFiber, (result) =>
                {
                    cancellation.Cancel();
                    Assert.AreEqual("bye", result);
                    testThread.Stop();
                });
                channel.Publish(new RequestReplyChannelRequest<string, string>("hello", replyChannel));
                disposables.AppendDisposable(disposableReply);
                testThread.Run();
            }
            timer.Dispose();
        }

        [Test]
        public void ShouldIncreasePoolFiberSubscriberCountByOne()
        {
            var subscriptions = new Subscriptions();
            var fiber = new PoolFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var unsubscriber = subscriptions.BeginSubscription();
            var disposableChannel = channel.Subscribe(fiber, x => { });
            unsubscriber.AppendDisposable(disposableChannel);

            Assert.AreEqual(1, subscriptions.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            subscriptions.Dispose();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreasedAnotherThreadPoolFiberSubscriberCountByOne()
        {
            var threadPool = UserThreadPool.StartNew(1);
            var fiber = threadPool.CreateFiber();
            var subscriptions = new Subscriptions();
            var channel = new Channel<int>();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var unsubscriber = subscriptions.BeginSubscription();
            var disposableChannel = channel.Subscribe(fiber, x => { });
            unsubscriber.AppendDisposable(disposableChannel);

            Assert.AreEqual(1, subscriptions.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            subscriptions.Dispose();
            threadPool.Dispose();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreaseConcurrentQueueActionQueueSubscriberCountByOne()
        {
            var subscriptions = new Subscriptions();
            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));
            var channel = new Channel<int>();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var unsubscriber = subscriptions.BeginSubscription();
            var disposableChannel = channel.Subscribe(fiber, x => { });
            unsubscriber.AppendDisposable(disposableChannel);

            Assert.AreEqual(1, subscriptions.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            subscriptions.Dispose();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void UnsubscriptionShouldRemoveSubscriber()
        {
            var subscriptions = new Subscriptions();
            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));
            var channel = new Channel<int>();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);

            var unsubscriber = subscriptions.BeginSubscription();
            var disposableChannel = channel.Subscribe(fiber, x => { });
            unsubscriber.AppendDisposable(disposableChannel);

            Assert.AreEqual(1, subscriptions.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            unsubscriber.Dispose();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }
    }
}