using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using System.Linq;
using System.Threading.Tasks;

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
                var filters = new List<Filter<MessageFrame>>();
                filters.Add((msg) =>
                {
                    return msg.NodeId != node.NodeId;
                });
                var filter = new MessageFilter<MessageFrame>(filters, node.Fiber, (msg) =>
                {
                    node.ReceivedMessages.Add(msg);
                });
                channel.Subscribe(node.Fiber, filter.Receive);
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
        public void CallAndResponse()
        {
            var channelCall = new Channel<MessageFrame>();
            var channelResponse = new Channel<MessageFrame>();

            int multiCount = 5;
            var nodeList = CreateNodeList<MessageFrame>(multiCount);

            foreach (var node in nodeList)
            {
                channelCall.Subscribe(node.Fiber, (msg) =>
                {
                    channelResponse.Publish(new MessageFrame()
                    {
                        NodeId = node.NodeId,
                        Message = msg.Message.Split(new char[] { ' ' }, 2)[1],
                    });
                });
                channelResponse.Subscribe(node.Fiber, (msg) =>
                {
                    node.ReceivedMessages.Add(msg);
                });
            }

            channelCall.Publish(new MessageFrame() { NodeId = 2, Message = "Say Ho" });
            channelCall.Publish(new MessageFrame() { NodeId = 2, Message = "Say Ho,Ho" });

            Thread.Sleep(10);

            foreach (var node in nodeList)
            {
                Assert.AreEqual(2 * multiCount, node.ReceivedMessages.Count);
                Assert.AreEqual(multiCount, node.ReceivedMessages.Count(x => x.Message == "Ho"));
                Assert.AreEqual(multiCount, node.ReceivedMessages.Count(x => x.Message == "Ho,Ho"));
            }
        }

        [Test]
        public void AsyncHandler()
        {
            var channel = new Channel<int>();

            int multiCount = 5;
            var nodeList = CreateNodeList<int>(multiCount);

            foreach (var node in nodeList)
            {
                channel.Subscribe(node.Fiber, async (msg) =>
                {
                    if (msg > 0)
                    {
                        await Task.Delay(msg).ConfigureAwait(false);
                    }
                    node.ReceivedMessages.Add(msg);
                    return () => { };
                });
            }

            channel.Publish(20);
            channel.Publish(0);

            Thread.Sleep(50);

            foreach (var node in nodeList)
            {
                Assert.AreEqual(node.ReceivedMessages.Count, 2);
                Assert.AreEqual(20, node.ReceivedMessages[0]);
                Assert.AreEqual(0, node.ReceivedMessages[1]);
            }
        }
    }

    class Node<T>
    {
        public readonly int NodeId;
        public readonly PoolFiberSlim Fiber;
        public readonly List<T> ReceivedMessages = new List<T>();

        public Node(int nodeId)
        {
            this.NodeId = nodeId;
            this.Fiber = new PoolFiberSlim();
        }
    }

    class MessageFrame
    {
        public int NodeId;
        public string Message;
    }
}
