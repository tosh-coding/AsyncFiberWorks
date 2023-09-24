using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Retlang.Channels;
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
                fiber.Start();
                var channel = new Channel<string>();

                var reset = new AutoResetEvent(false);
                channel.Subscribe(fiber, delegate { reset.Set(); });
                channel.Publish("hello");

                Assert.IsTrue(reset.WaitOne(5000, false));
            }
        }

        [Test]
        public void PersistentPubSubWithPool()
        {
            //PoolFiber uses the .NET thread pool by default
            using (var fiber = new PoolFiber())
            {
                fiber.Start();
                var channel = new Channel<string>();

                var reset = new AutoResetEvent(false);
                channel.PersistentSubscribe(fiber, delegate { reset.Set(); });
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
                channel.Subscribe(fiber, delegate { reset.Set(); });
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
                var sub = new ChannelSubscription<int>(fiber, onMsg, filter);
                channel.SubscribeOnProducerThreads(fiber.FallbackDisposer, sub);
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

                counter.SubscribeToBatch(fiber, cb, 1);

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
                counter.SubscribeToKeyedBatch(fiber, keyResolver, cb, 0);

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
            using (var fiber = new PoolFiber())
            {
                fiber.Start();
                var channel = new RequestReplyChannel<string, string>();
                channel.Subscribe(fiber, req => req.SendReply("bye"));
                var reply = channel.SendRequest("hello");

                string result;
                Assert.IsTrue(RequestReplyChannelTests.WaitReceiveForTest(reply, 10000, out result));
                Assert.AreEqual("bye", result);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Snapshot(int responderType)
        {
            using (var fiberReply = new PoolFiber())
            {
                fiberReply.Start();
                var channel = new SnapshotChannel<int>();
                var lockerResponseValue = new object();

                // A value managed by the responder.
                int currentValue = 0;

                // Set up responder. 
                if (responderType == 0)
                {
                    channel.ReplyToPrimingRequest(fiberReply, () =>
                    {
                        lock (lockerResponseValue)
                        {
                            return currentValue;
                        }
                    });
                    Assert.AreEqual(1, channel.NumSubscribers);
                    Assert.AreEqual(0, channel.NumPersistentSubscribers);
                }
                else
                {
                    channel.PersistentReplyToPrimingRequest(fiberReply, () =>
                    {
                        lock (lockerResponseValue)
                        {
                            return currentValue;
                        }
                    });
                    Assert.AreEqual(1, channel.NumSubscribers);
                    Assert.AreEqual(1, channel.NumPersistentSubscribers);
                }

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
                var fiberRequest = new StubFiber();
                var cancellation = new CancellationTokenSource();
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

                            cancellation.Cancel();
                        }, 200);
                    }
                };
                Action<int> actionReceive = (v) =>
                {
                    receivedValues.Add(v);
                    Console.WriteLine("Received: " + v);
                };
                var handleReceive = channel.PrimedSubscribe(
                    fiberRequest, actionControl, actionReceive, 5000);

                fiberRequest.ExecuteUntilCanceled(cancellation.Token);
                handleReceive.Dispose();
            }
        }

        [Test]
        public void ShouldIncreasePoolFiberSubscriberCountByOne()
        {
            var fiber = PoolFiber.StartNew();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            channel.Subscribe(fiber, x => { });

            Assert.AreEqual(1, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            fiber.Dispose();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreaseThreadFiberSubscriberCountByOne()
        {
            var fiber = ThreadFiber.StartNew();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            channel.Subscribe(fiber, x => { });

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
            channel.Subscribe(fiber, x => { });

            Assert.AreEqual(1, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            fiber.Dispose();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void UnsubscriptionShouldRemoveSubscriber()
        {
            var fiber = PoolFiber.StartNew();
            var channel = new Channel<int>();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
            var unsubscriber = channel.Subscribe(fiber, x => { });

            Assert.AreEqual(1, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(1, channel.NumSubscribers);
            unsubscriber.Dispose();

            Assert.AreEqual(0, fiber.FallbackDisposer.NumSubscriptions);
            Assert.AreEqual(0, channel.NumSubscribers);
        }
    }
}