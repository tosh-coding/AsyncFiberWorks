using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Retlang.Channels;
using Retlang.Core;
using Retlang.Fibers;

namespace RetlangTests.Examples
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
                var subscriber = new ChannelSubscription<string>(fiber, delegate { reset.Set(); });
                subscriber.Subscribe(channel);
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
                var subscriber = new ChannelSubscription<string>(fiber, delegate { reset.Set(); });
                subscriber.Subscribe(channel);
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
                var subscriber = new ChannelSubscription<int>(fiber, onMsg, filter);
                subscriber.Subscribe(channel);
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

                var subscriber = new BatchSubscriber<int>(fiber, cb, 1, null);
                subscriber.Subscribe(counter);

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

                Converter<int, String> keyResolver = x => x.ToString();
                var subscriber = new KeyedBatchSubscriber<string, int>(keyResolver, cb, fiber, 0, null);
                subscriber.Subscribe(counter);

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
            var testThread = new ConsumingThread();
            var testFiber = new PoolFiber(testThread, new DefaultExecutor());

            using (var fiber = new PoolFiber())
            {
                var channel = new RequestReplyChannel<string, string>();
                var subscriber = new RequestReplyChannelSubscriber<string, string>(fiber, req => req.SendReply("bye"));
                subscriber.Subscribe(channel);

                var reply = channel.SendRequest("hello");
                reply.SetCallbackOnReceive(10000, testFiber, (_) =>
                {
                    string result;
                    bool received = reply.TryReceive(out result);
                    Assert.IsTrue(received);
                    Assert.AreEqual("bye", result);
                    testThread.Stop();
                });
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
                var subscriber = new RequestReplyChannelSubscriber<object, int>(fiberReply, request => request.SendReply(reply()));
                channel.ReplyToPrimingRequest(subscriber);
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
                var FiberRequestConsumer = new ConsumingThread();
                var fiberRequest = new PoolFiber(FiberRequestConsumer, new DefaultExecutor());
                var receivedValues = new List<int>();
                Action<SnapshotRequestControlEvent> actionControl = (controlEvent) =>
                {
                    if (controlEvent == SnapshotRequestControlEvent.Timeout)
                    {
                        Assert.Fail("SnapshotRequestControlEvent.Timeout");
                    }
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

                            FiberRequestConsumer.Stop();
                        }, 200);
                    }
                };
                Action<int> actionReceive = (v) =>
                {
                    receivedValues.Add(v);
                    Console.WriteLine("Received: " + v);
                };
                var requester = new SnapshotRequest<int>(fiberRequest, actionControl, actionReceive, 5000);
                requester.Subscribe(channel);
                var handleReceive = requester;

                FiberRequestConsumer.Run();
                handleReceive.Dispose();
            }
        }

        [Test]
        public void ShouldIncreasePoolFiberSubscriberCountByOne()
        {
            var fiber = new PoolFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var subscriber = new ChannelSubscription<int>(fiber, x => { });
            subscriber.Subscribe(channel);

            Assert.AreEqual(1, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            fiber.Dispose();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreaseThreadFiberSubscriberCountByOne()
        {
            var fiber = new ThreadFiber();
            fiber.Start();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var subscriber = new ChannelSubscription<int>(fiber, x => { });
            subscriber.Subscribe(channel);

            Assert.AreEqual(1, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            fiber.Dispose();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreaseStubFiberSubscriberCountByOne()
        {
            var fiber = new StubFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var subscriber = new ChannelSubscription<int>(fiber, x => { });
            subscriber.Subscribe(channel);

            Assert.AreEqual(1, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            fiber.Dispose();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void UnsubscriptionShouldRemoveSubscriber()
        {
            var fiber = new PoolFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);

            var subscriber = new ChannelSubscription<int>(fiber, x => { });
            subscriber.Subscribe(channel);

            Assert.AreEqual(1, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            subscriber.Dispose();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }
    }
}