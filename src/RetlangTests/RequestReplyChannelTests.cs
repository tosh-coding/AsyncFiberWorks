using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        [Test]
        public void PersistentSubscriber()
        {
            var responder = PoolFiber.StartNew();
            var timeCheck = new RequestReplyChannel<string, DateTime>();
            var now = DateTime.Now;
            Action<IRequest<string, DateTime>> onRequest = req => req.SendReply(now);
            timeCheck.PersistentSubscribe(responder, onRequest);
            Assert.AreEqual(1, timeCheck.NumPersistentSubscribers);
            Assert.AreEqual(1, timeCheck.NumSubscribers);
            IRequestPublisher<string, DateTime> requester = timeCheck;
            var response = requester.SendRequest("hello");
            DateTime result;
            Assert.IsTrue(WaitReceiveForTest(response, 10000, out result));
            Assert.AreEqual(result, now);
        }

        [Test]
        public void ChangeResponseSync()
        {
            IFiber responder = PoolFiber.StartNew();
            var countChannel = new RequestReplyChannel<string, int>();

            var dic = new (string, int)[] {
                ("apple", 100),
                ("banana", 200),
                ("carrot", 300),
            }.ToDictionary(x => x.Item1, x => x.Item2);

            Action<IRequest<string, int>> onRequest =
                delegate (IRequest<string, int> req)
                {
                    Thread.Sleep(20);
                    if (dic.TryGetValue(req.Request, out int value))
                    {
                        req.SendReply(value);
                    }
                    else
                    {
                        req.SendReply(-1);
                    }
                };
            countChannel.Subscribe(responder, onRequest);

            var requests = new List<string>();
            requests.AddRange(dic.Keys);
            requests.Add("daikon");
            int indexRequest = 0;

            int timeoutInMs = 500;
            IReply<int> response = null;
            var mainFiber = new StubFiber();
            var cancellation = new CancellationTokenSource();
            var ownAction = new List<Action>();
            Action action = () =>
            {
                response?.Dispose();
                if (indexRequest >= requests.Count)
                {
                    mainFiber.Enqueue(() => { cancellation.Cancel(); });
                }
                else
                {
                    string requestData = requests[indexRequest];
                    indexRequest += 1;
                    response = countChannel.SendRequest(requestData);
                    response.SetCallbackOnReceive(timeoutInMs, mainFiber, (_) =>
                    {
                        bool isReceived = response.TryReceive(out int responseData);
                        Assert.IsTrue(isReceived);
                        if (dic.ContainsKey(requestData))
                        {
                            Assert.AreEqual(dic[requestData], responseData);
                        }
                        else
                        {
                            Assert.AreEqual(-1, responseData);
                        }
                        mainFiber.Enqueue(ownAction[0]);
                    });
                }
            };
            ownAction.Add(action);
            mainFiber.Enqueue(action);
            mainFiber.ExecuteUntilCanceled(cancellation.Token);
            Assert.AreEqual(requests.Count, indexRequest);
        }

        [Test]
        public async Task ChangeResponseAsync()
        {
            IFiber responder = PoolFiber.StartNew();
            var countChannel = new RequestReplyChannel<string, int>();

            var dic = new (string, int)[] {
                ("apple", 100),
                ("banana", 200),
                ("carrot", 300),
            }.ToDictionary(x => x.Item1, x => x.Item2);

            Action<IRequest<string, int>> onRequest =
                (IRequest<string, int> req) =>
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(20);
                        await responder.SwitchTo();
                        if (dic.TryGetValue(req.Request, out int value))
                        {
                            req.SendReply(value);
                        }
                        else
                        {
                            req.SendReply(-1);
                        }
                    });
                };
            countChannel.Subscribe(responder, onRequest);

            var requests = new List<string>();
            requests.AddRange(dic.Keys);
            requests.Add("daikon");

            var mainFiber = PoolFiber.StartNew();

            foreach (var requestData in requests)
            {
                using (var response = countChannel.SendRequest(requestData))
                {
                    try
                    {
                        int responseData = await WaitOnReceive(response, 500);
                        await mainFiber.SwitchTo();
                        if (dic.ContainsKey(requestData))
                        {
                            Assert.AreEqual(dic[requestData], responseData);
                        }
                        else
                        {
                            Assert.AreEqual(-1, responseData);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        private static Task<T> WaitOnReceive<T>(IReply<T> reply, int timeoutInMs)
        {
            var tcs = new TaskCompletionSource<T>();
            reply.SetCallbackOnReceive(timeoutInMs, null, (_) =>
            {
                bool isReceived = reply.TryReceive(out T responseData);
                if (isReceived)
                {
                    tcs.SetResult(responseData);
                }
                else
                {
                    tcs.SetCanceled();
                }
            });
            return tcs.Task;
        }
    }
}