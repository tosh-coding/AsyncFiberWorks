using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.PubSub;
using AsyncFiberWorks.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests.Examples
{
    [TestFixture]
    public class PubSubExamples
    {
        [Test]
        public void PubSubDirect()
        {
            var channel = new Channel<string>();

            using (var composite = new CompositeDisposable())
            {
                // Subscribe
                var fiber = new PoolFiber();
                var resetEvent = new AutoResetEvent(false);
                Action<string> handler = (msg) =>
                {
                    resetEvent.Set();
                };
                var d = channel.Subscribe(fiber, handler);
                composite.Add(d);

                // Publish
                channel.Publish("hello");

                Assert.IsTrue(resetEvent.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithLocator()
        {
            var publisher = ChannelLocator.GetPublisher<string>();
            var subscriber = ChannelLocator.GetSubscriber<string>();

            using (var composite = new CompositeDisposable())
            {
                // Subscribe
                var fiber = new PoolFiber();
                var resetEvent = new AutoResetEvent(false);
                Action<string> handler = (msg) =>
                {
                     resetEvent.Set();
                };
                var d = subscriber.Subscribe(fiber, handler);
                composite.Add(d);

                // Publish
                publisher.Publish("hello");

                Assert.IsTrue(resetEvent.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithDedicatedThread()
        {
            var channel = new Channel<string>();

            using (var threadPool = UserThreadPool.StartNew(1))
            using (var composite = new CompositeDisposable())
            {
                // Subscribe
                var fiber = new PoolFiber(threadPool);
                var resetEvent = new AutoResetEvent(false);
                Action<string> handler = (msg) =>
                {
                    resetEvent.Set();
                };
                var d = channel.Subscribe(fiber, handler);
                composite.Add(d);

                // Publish
                channel.Publish("hello");

                Assert.IsTrue(resetEvent.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithAnotherThreadPool()
        {
            var channel = new Channel<string>();

            using (var composite = new CompositeDisposable())
            {
                // Subscribe
                var fiber = AnotherThreadPool.Instance.CreateFiber();
                var reset = new AutoResetEvent(false);
                Action<string> handler = (msg) =>
                {
                    reset.Set();
                };
                var d = channel.Subscribe(fiber, handler);
                composite.Add(d);

                // Publish
                channel.Publish("hello");

                Assert.IsTrue(reset.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubWithDedicatedThreadWithFilter()
        {
            var channel = new Channel<int>();

            using (var threadPool = UserThreadPool.StartNew())
            using (var composite = new CompositeDisposable())
            {
                // Subscribe
                var fiber = threadPool.CreateFiber();
                var builder = new MessageReceiverBuilder<int>();
                builder.AddFilter(x => x % 2 == 0);
                var resetEvent = new AutoResetEvent(false);
                Action<int> onMsg = x =>
                {
                    Assert.IsTrue(x % 2 == 0);
                    if (x == 4)
                    {
                        resetEvent.Set();
                    }
                };
                var handler = builder.Build(fiber, onMsg);
                var d = channel.Subscribe(fiber, handler);
                composite.Add(d);

                // Publish
                channel.Publish(1);
                channel.Publish(2);
                channel.Publish(3);
                channel.Publish(4);

                Assert.IsTrue(resetEvent.WaitOne(5000, false));
            }
        }

        [Test]
        public void PubSubBatchingOnSubscriber()
        {
            var channel = new Channel<int>();

            using (var threadPool = UserThreadPool.StartNew())
            using (var composite = new CompositeDisposable())
            {
                // Subscribe
                var fiber = threadPool.CreateFiber();
                var resetEvent = new ManualResetEvent(false);
                var total = 0;
                Action<IList<int>> handler = delegate (IList<int> batch)
                {
                    total += batch.Count;
                    if (total == 10)
                    {
                        resetEvent.Set();
                    }
                };
                var filter = new BatchFilter<int>(1, fiber, handler);
                var d = channel.Subscribe(fiber, filter.Receive);
                composite.Add(filter, d);

                // Publish
                for (var i = 0; i < 10; i++)
                {
                    channel.Publish(i);
                }

                Assert.IsTrue(resetEvent.WaitOne(10000, false));
            }
        }

        [Test]
        public void PubSubBatchingWithKeyOnSubscriber()
        {
            var channel = new Channel<int>();

            using (var threadPool = UserThreadPool.StartNew())
            using (var composite = new CompositeDisposable())
            {
                // Subscribe
                var fiber = threadPool.CreateFiber();
                var resetEvent = new ManualResetEvent(false);
                Action<IDictionary<String, int>> cb = delegate(IDictionary<String, int> batch)
                {
                    if (batch.ContainsKey("9"))
                    {
                        resetEvent.Set();
                    }
                };
                Converter<int, String> keyResolver = x => x.ToString();
                var filter = new KeyedBatchFilter<string, int>(keyResolver, 0, fiber, cb);
                composite.Add(filter);
                var d = channel.Subscribe(fiber, filter.Receive);
                composite.Add(d);

                // Publish
                for (var i = 0; i < 10; i++)
                {
                    channel.Publish(i);
                }

                Assert.IsTrue(resetEvent.WaitOne(10000, false));
            }
        }

        [Test]
        public void PubSubWithLastFilter()
        {
            var channel = new Channel<int>();

            using (var threadPool = UserThreadPool.StartNew())
            using (var composite = new CompositeDisposable())
            {
                // Subscribe
                var fiber = threadPool.CreateFiber();
                var resetEvent = new ManualResetEvent(false);
                var total = 0;
                Action<int> handler = delegate (int msg)
                {
                    total += msg;
                    if (msg == 10000)
                    {
                        resetEvent.Set();
                    }
                };
                var filter = new LastFilter<int>(10, fiber, handler);
                var d = channel.Subscribe(fiber, filter.Receive);
                composite.Add(filter, d);

                // Publish
                channel.Publish(1);
                channel.Publish(10);
                Thread.Sleep(30);
                channel.Publish(100);
                channel.Publish(1000);
                channel.Publish(10000);

                Assert.IsTrue(resetEvent.WaitOne(10000, false));
                Assert.AreEqual(10 + 10000, total);
            }
        }

        [Test]
        public void KeyedPubSub()
        {
            var publisher = ChannelLocator.GetPublisher<Guid, string>();
            var subscriber = ChannelLocator.GetSubscriber<Guid, string>();
            var guidA = Guid.NewGuid();
            var guidB = Guid.NewGuid();

            using (var composite = new CompositeDisposable())
            {
                // Subscribe1
                var fiber1 = new PoolFiber();
                var reset1 = new AutoResetEvent(false);
                int helloCount1 = 0;
                var d1 = subscriber.Subscribe(guidA, fiber1, (msg) =>
                {
                    if (msg == "hello")
                    {
                        helloCount1 += 1;
                    }
                    else if (msg == "end")
                    {
                        reset1.Set();
                    }
                });
                composite.Add(d1);

                // Subscribe2
                var fiber2 = new PoolFiber();
                var reset2 = new AutoResetEvent(false);
                int helloCount2 = 0;
                var d2 = subscriber.Subscribe(guidB, fiber2, (msg) =>
                {
                    if (msg == "hello")
                    {
                        helloCount2 += 1;
                    }
                    else if (msg == "end")
                    {
                        reset2.Set();
                    }
                });
                composite.Add(d2);

                // Subscribe3
                var fiber3 = new PoolFiber();
                var reset3 = new AutoResetEvent(false);
                int helloCount3 = 0;
                var d3 = subscriber.Subscribe(guidA, fiber3, (msg) =>
                {
                    if (msg == "hello")
                    {
                        helloCount3 += 1;
                    }
                    else if (msg == "end")
                    {
                        reset3.Set();
                    }
                });
                composite.Add(d3);

                // Publish
                publisher.Publish(guidA, "hello");
                publisher.Publish(guidB, "hello");
                publisher.Publish(guidA, "hello");
                publisher.Publish(guidA, "hello");
                publisher.Publish(guidA, "end");
                publisher.Publish(guidB, "end");

                reset1.WaitOne(5000, false);
                reset2.WaitOne(5000, false);
                reset3.WaitOne(5000, false);

                Assert.AreEqual(helloCount1, 3);
                Assert.AreEqual(helloCount2, 1);
                Assert.AreEqual(helloCount3, 3);
            }
        }
    }
}