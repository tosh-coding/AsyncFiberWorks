using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Retlang.Channels;
using Retlang.Fibers;

namespace RetlangTests
{
    [TestFixture]
    public class RequestReplyChannelTests
    {
        [Test]
        public void SynchronousRequestReply()
        {
            var responder = PoolFiber.StartNew();
            var timeCheck = new RequestReplyChannel<string, DateTime>();
            var now = DateTime.Now;
            Action<IRequest<string, DateTime>> onRequest = req => req.SendReply(now);
            timeCheck.Subscribe(responder, onRequest);
            var response = timeCheck.SendRequest("hello");
            DateTime result;
            Assert.IsTrue(WaitReceiveForTest(response, 10000, out result));
            Assert.AreEqual(result, now);
        }

        public static bool WaitReceiveForTest<T>(IReply<T> reply, int timeoutInMs, out T result)
        {
            var sw = Stopwatch.StartNew();
            while (true)
            {
                if (reply.TryReceive(out result))
                {
                    return true;
                }
                if (sw.ElapsedMilliseconds >= timeoutInMs)
                {
                    return false;
                }
                // Blocking should be avoided. Here we use it for simplifying the test code.
                Thread.Sleep(100);
            }
        }

        [Test]
        public void SynchronousRequestWithMultipleReplies()
        {
            IFiber responder = PoolFiber.StartNew();
            var countChannel = new RequestReplyChannel<string, int>();

            var allSent = new AutoResetEvent(false);
            Action<IRequest<string, int>> onRequest =
                delegate(IRequest<string, int> req)
                {
                    for (var i = 0; i <= 5; i++)
                        req.SendReply(i);
                    allSent.Set();
                };
            countChannel.Subscribe(responder, onRequest);
            var response = countChannel.SendRequest("hello");
            int result;
            using (response)
            {
                for (var i = 0; i < 5; i++)
                {
                    Assert.IsTrue(WaitReceiveForTest(response, 1000, out result));
                    Assert.AreEqual(result, i);
                }
                allSent.WaitOne(10000, false);
            }
            Assert.IsTrue(WaitReceiveForTest(response, 3000, out result));
            Assert.AreEqual(5, result);
            Assert.IsFalse(WaitReceiveForTest(response, 3000, out result));
        }
    }
}