using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.PubSub;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using System.Linq;
using System.Threading.Tasks;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class ChannelTests
    {
        [Test]
        public void BroadcastMessage()
        {
            var channel = new Channel<string>();

            int multiCount = 5;
            var nodeList = CreateNodeList<string>(multiCount);

            foreach (var node in nodeList)
            {
                channel.Subscribe(node.Fiber, (msg) =>
                {
                    node.ReceivedMessages.Add(msg);
                });
            }

            channel.Publish("Hello");
            channel.Publish("World");

            Thread.Sleep(10);

            foreach (var node in nodeList)
            {
                Assert.AreEqual(node.ReceivedMessages.Count, 2);
                Assert.AreEqual("Hello", node.ReceivedMessages[0]);
                Assert.AreEqual("World", node.ReceivedMessages[1]);
            }
        }

        Node<T>[] CreateNodeList<T>(int multiCount)
        {
            var nodeList = new Node<T>[multiCount];
            for (int i = 0; i < nodeList.Length; i++)
            {
                nodeList[i] = new Node<T>(i);
            }
            return nodeList;
        }

        [Test]
        public void OneToMulti()
        {
            var channel = new Channel<MessageFrame>();

            int multiCount = 5;
            var nodeList = CreateNodeList<MessageFrame>(multiCount);

            foreach (var node in nodeList)
            {
                var rb = new MessageReceiverBuilder<MessageFrame>();
                rb.AddFilter((msg) =>
                {
                    return msg.NodeId != node.NodeId;
                });
                var receiver = rb.Build(node.Fiber, (msg) =>
                {
                    node.ReceivedMessages.Add(msg);
                });
                channel.Subscribe(node.Fiber, receiver);
            }

            channel.Publish(new MessageFrame() { NodeId = 2, Message = "Hello" });
            channel.Publish(new MessageFrame() { NodeId = 2, Message = "World" });

            Thread.Sleep(10);

            foreach (var node in nodeList)
            {
                if (node.NodeId == 2)
                {
                    Assert.AreEqual(0, node.ReceivedMessages.Count);
                }
                else
                {
                    Assert.AreEqual(2, node.ReceivedMessages.Count);
                    Assert.AreEqual("Hello", node.ReceivedMessages[0].Message);
                    Assert.AreEqual("World", node.ReceivedMessages[1].Message);
                }
            }
        }

        [Test]
        public async Task AsyncHandler()
        {
            var channel = new Channel<int>();

            int multiCount = 5;
            var nodeList = CreateNodeList<int>(multiCount);

            foreach (var node in nodeList)
            {
                channel.Subscribe(node.Fiber, (e, msg) =>
                {
                    e.PauseWhileRunning(async () =>
                    {
                        if (msg > 0)
                        {
                            await Task.Delay(msg).ConfigureAwait(false);
                        }
                        node.ReceivedMessages.Add(msg);
                    });
                });
            }

            channel.Publish(20);
            channel.Publish(0);

            var drainTask = Task.WhenAll(nodeList.Select(node => node.Fiber.EnqueueAsync(() => { })));
            var completed = await Task.WhenAny(drainTask, Task.Delay(3000)).ConfigureAwait(false);
            Assert.AreSame(drainTask, completed, "Timed out waiting for fibers to drain.");
            await drainTask.ConfigureAwait(false);

            foreach (var node in nodeList)
            {
                Assert.AreEqual(2, node.ReceivedMessages.Count);
                Assert.AreEqual(20, node.ReceivedMessages[0]);
                Assert.AreEqual(0, node.ReceivedMessages[1]);
            }
        }

        [Test]
        public void ShouldIncreasePoolFiberSubscriberCountByOne()
        {
            var fiber = new PoolFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, channel.NumSubscribers);
            var disposableChannel = channel.Subscribe(fiber, x => { });

            Assert.AreEqual(1, channel.NumSubscribers);
            disposableChannel.Dispose();

            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreasedAnotherThreadPoolFiberSubscriberCountByOne()
        {
            var threadPool = UserThreadPool.StartNew(1);
            var fiber = threadPool.CreateFiber();
            var channel = new Channel<int>();

            Assert.AreEqual(0, channel.NumSubscribers);
            var disposableChannel = channel.Subscribe(fiber, x => { });

            Assert.AreEqual(1, channel.NumSubscribers);
            disposableChannel.Dispose();
            threadPool.Dispose();

            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void ShouldIncreaseConcurrentQueueActionQueueSubscriberCountByOne()
        {
            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));
            var channel = new Channel<int>();

            Assert.AreEqual(0, channel.NumSubscribers);
            var disposableChannel = channel.Subscribe(fiber, x => { });

            Assert.AreEqual(1, channel.NumSubscribers);
            disposableChannel.Dispose();

            Assert.AreEqual(0, channel.NumSubscribers);
        }

        [Test]
        public void UnsubscriptionShouldRemoveSubscriber()
        {
            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));
            var channel = new Channel<int>();

            Assert.AreEqual(0, channel.NumSubscribers);

            var disposableChannel = channel.Subscribe(fiber, x => { });

            Assert.AreEqual(1, channel.NumSubscribers);
            disposableChannel.Dispose();

            Assert.AreEqual(0, channel.NumSubscribers);
        }
    }

    class Node<T>
    {
        public readonly int NodeId;
        public readonly PoolFiber Fiber;
        public readonly List<T> ReceivedMessages = new List<T>();

        public Node(int nodeId)
        {
            this.NodeId = nodeId;
            this.Fiber = new PoolFiber();
        }
    }

    class MessageFrame
    {
        public int NodeId;
        public string Message;
    }
}
