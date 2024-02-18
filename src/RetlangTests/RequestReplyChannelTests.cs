using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Retlang.Channels;
using Retlang.Core;
using Retlang.Fibers;

namespace RetlangTests
{
    [TestFixture]
    public class RequestReplyChannelTests
    {
        [Test]
        public void SynchronousRequestReply()
        {
            var responder = new PoolFiber();
            var timeCheck = new RequestReplyChannel<string, DateTime>();
            var now = DateTime.Now;
            Action<IRequest<string, DateTime>> onRequest = req => req.SendReply(now);
            var subscriber = new RequestReplyChannelSubscriber<string, DateTime>(responder, onRequest);
            subscriber.Subscribe(timeCheck);

            {
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var requesterFiber = new PoolFiber(requesterThread, new DefaultExecutor());
                requesterFiber.Pause();
                var response = timeCheck.SendRequest("hello");
                response.SetCallbackOnReceive(10000, new PoolFiber(), (_) =>
                {
                    requesterFiber.Resume(() =>
                    {
                        bool received = response.TryReceive(out DateTime result);
                        Assert.IsTrue(received);
                        Assert.AreEqual(result, now);
                        requesterThread.Stop();
                    });
                });
                requesterThread.Run();
            }
        }

        [Test]
        public void SynchronousRequestWithMultipleReplies()
        {
            IFiber responder = new PoolFiber();
            var countChannel = new RequestReplyChannel<string, int>();

            var allSent = new AutoResetEvent(false);
            Action<IRequest<string, int>> onRequest =
                delegate(IRequest<string, int> req)
                {
                    for (var i = 0; i <= 5; i++)
                        req.SendReply(i);
                    allSent.Set();
                };
            var subscriber = new RequestReplyChannelSubscriber<string, int>(responder, onRequest);
            subscriber.Subscribe(countChannel);

            {
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var requesterFiber = new PoolFiber(requesterThread, new DefaultExecutor());
                requesterFiber.Pause();
                var response = countChannel.SendRequest("hello");
                allSent.WaitOne(10000, false);
                using (response)
                {
                    int i = 0;
                    Action[] receivingArray = new Action[1];
                    Action receiving = () =>
                    {
                        requesterFiber.Resume(() =>
                        {
                            int result;
                            bool received = response.TryReceive(out result);
                            Assert.IsTrue(received);
                            Assert.AreEqual(result, i);
                            i += 1;
                            requesterFiber.Enqueue(() =>
                            {
                                if (i < 5)
                                {
                                    requesterFiber.Pause();
                                    response.SetCallbackOnReceive(1000, new PoolFiber(), (_) =>
                                    {
                                        receivingArray[0].Invoke();
                                    });
                                }
                                else
                                {
                                    requesterFiber.Pause();
                                    response.SetCallbackOnReceive(3000, new PoolFiber(), (_) =>
                                    {
                                        requesterFiber.Resume(() =>
                                        {
                                            requesterFiber.Enqueue(() =>
                                            {
                                                received = response.TryReceive(out result);
                                                Assert.IsTrue(received);
                                                Assert.AreEqual(5, result);

                                                requesterFiber.Pause();
                                                response.SetCallbackOnReceive(3000, new PoolFiber(), (dummy) =>
                                                {
                                                    requesterFiber.Resume(() =>
                                                    {
                                                        received = response.TryReceive(out result);
                                                        Assert.IsFalse(received);
                                                        requesterThread.Stop();
                                                    });
                                                });
                                            });
                                        });
                                    });
                                }
                            });
                        });
                    };
                    receivingArray[0] = receiving;
                    response.SetCallbackOnReceive(1000, new PoolFiber(), (_) => receiving());
                    requesterThread.Run();
                }
            }
        }

        [Test]
        public void ChangeResponseSync()
        {
            IFiber responder = new PoolFiber();
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
            var subscriber = new RequestReplyChannelSubscriber<string, int>(responder, onRequest);
            subscriber.Subscribe(countChannel);

            var requests = new List<string>();
            requests.AddRange(dic.Keys);
            requests.Add("daikon");
            int indexRequest = 0;

            int timeoutInMs = 500;
            IReply<int> response = null;
            var mainThread = new ThreadPoolAdaptorFromQueueForThread();
            var mainFiber = new PoolFiber(mainThread, new DefaultExecutor());
            var ownAction = new List<Action>();
            Action action = () =>
            {
                response?.Dispose();
                if (indexRequest >= requests.Count)
                {
                    mainFiber.Enqueue(() => { mainThread.Stop(); });
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
            mainThread.Run();
            Assert.AreEqual(requests.Count, indexRequest);
        }

        [Test]
        public async Task ChangeResponseAsync()
        {
            IFiber responder = new PoolFiber();
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
            var subscriber = new RequestReplyChannelSubscriber<string, int>(responder, onRequest);
            subscriber.Subscribe(countChannel);

            var requests = new List<string>();
            requests.AddRange(dic.Keys);
            requests.Add("daikon");

            var mainFiber = new PoolFiber();

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