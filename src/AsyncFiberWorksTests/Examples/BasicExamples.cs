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
                var subscriber = new ChannelSubscription<string>(fiber, delegate { reset.Set(); });
                var subscriptionChannel = channel.Subscribe(subscriber);
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
                var subscriber = new ChannelSubscription<string>(fiber, delegate { reset.Set(); });
                var subscriptionChannel = channel.Subscribe(subscriber);
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
                var filter = new MessageFilter<int>();
                filter.AddFilterOnProducerThread(x => x % 2 == 0);
                var subscriptionFiber = fiber.BeginSubscription();
                var subscriber = new ChannelSubscription<int>(filter, fiber, onMsg);
                var subscriptionChannel = channel.Subscribe(subscriber);
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
                var subscriber = new BatchSubscriber<int>(1, fiber, cb);
                var subscriptionChannel = counter.Subscribe(subscriber);
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
                var subscriber = new KeyedBatchSubscriber<string, int>(null, keyResolver, 0, fiber, cb);
                disposables.Add(subscriber);
                var subscriptionChannel = counter.Subscribe(subscriber);
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
                var subscriptionChannel = channel.Subscribe(
                    fiber.CreateAction<IRequest<string, string>>(req => req.ReplyTo.Publish("bye")));
                subscriptionFiber.AppendDisposable(subscriptionChannel);

                var disposables = new Unsubscriber();
                var timeoutTimer = testFiber.Schedule(() =>
                {
                    disposables.Dispose();
                    Assert.Fail();
                }, 10000);
                var replyChannel = new Channel<string>();
                var reply = replyChannel.Subscribe((result) => testFiber.Enqueue(() =>
                {
                    timeoutTimer.Dispose();
                    Assert.AreEqual("bye", result);
                    testThread.Stop();
                }));
                channel.Publish(new RequestReplyChannelRequest<string, string>("hello", replyChannel));
                disposables.AppendDisposable(reply);
                testThread.Run();
            }
        }

        [Test]
        public void Snapshot()
        {
            using (var fiberReply = new PoolFiber())
            {
                var channel = new SnapshotChannel<int>();
                var lockerResponseValue = new object();

                // A value managed by the responder.
                int currentValue = 0;

                // Set up responder. 
                Func<int> reply = () =>
                {
                    lock (lockerResponseValue)
                    {
                        return currentValue;
                    }
                };
                var subscriptionFiber = fiberReply.BeginSubscription();
                var subscriptionChannel = channel.ReplyToPrimingRequest(
                    fiberReply.CreateAction<IRequest<object, int>>(request => request.ReplyTo.Publish(reply())));
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                Assert.AreEqual(1, channel.NumSubscribers);

                // Start changing values.

                lock (lockerResponseValue)
                {
                    currentValue = 1;
                    channel.Publish(currentValue);
                }
                lock (lockerResponseValue)
                {
                    currentValue = 2;
                    channel.Publish(currentValue);
                }

                // Start requesting.
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var fiberRequest = new PoolFiber(requesterThread, new DefaultExecutor());
                var receivedValues = new List<int>();
                var timeoutTimerCancellation = new Unsubscriber();
                Action<SnapshotRequestControlEvent> actionControl = (controlEvent) =>
                {
                    timeoutTimerCancellation.Dispose();
                    if (controlEvent == SnapshotRequestControlEvent.Connecting)
                    {
                        return;
                    }
                    if (controlEvent == SnapshotRequestControlEvent.Connected)
                    {
                        lock (lockerResponseValue)
                        {
                            currentValue = 4;
                            channel.Publish(currentValue);
                        }
                        lock (lockerResponseValue)
                        {
                            currentValue = 8;
                            channel.Publish(currentValue);
                        }

                        fiberRequest.Schedule(() =>
                        {
                            // Finish.

                            int[] expectedReceiveValues = new int[]
                            {
                            2, 4, 8,
                            };

                            Assert.AreEqual(expectedReceiveValues.Length, receivedValues.Count);

                            for (int i = 0; i < expectedReceiveValues.Length; i++)
                            {
                                Assert.AreEqual(expectedReceiveValues[i], receivedValues[i]);
                            }

                            requesterThread.Stop();
                        }, 200);
                    }
                };
                Action<int> actionReceive = (v) =>
                {
                    receivedValues.Add(v);
                    Console.WriteLine("Received: " + v);
                };
                var handleReceive = channel.PrimedSubscribe(fiberRequest, actionControl, actionReceive);
                var timeoutTimer = fiberRequest.Schedule(() =>
                {
                    handleReceive.Dispose();
                    Assert.Fail("SnapshotRequestControlEvent.Timeout");
                }, 5000);
                timeoutTimerCancellation.AppendDisposable(timeoutTimer);

                requesterThread.Run();
                handleReceive.Dispose();
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
            var subscriber = new ChannelSubscription<int>(fiber, x => { });
            var subscriptionCHannel = channel.Subscribe(subscriber);
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
            var subscriber = new ChannelSubscription<int>(fiber, x => { });
            var subscriptionChannel = channel.Subscribe(subscriber);
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
            var subscriber = new ChannelSubscription<int>(fiber, x => { });
            var subscriptionChannel = channel.Subscribe(subscriber);
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
            var subscriber = new ChannelSubscription<int>(fiber, x => { });
            var subscriptionChannel = channel.Subscribe(subscriber);
            subscriptionFiber.AppendDisposable(subscriptionChannel);

            Assert.AreEqual(1, fiber.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            subscriptionFiber.Dispose();

            Assert.AreEqual(0, fiber.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }
    }
}