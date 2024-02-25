using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorksTests
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
            var actionWithContext = new ActionWithContext<IRequest<string, DateTime>>(onRequest, responder);
            var subscriber = timeCheck.AddResponder(actionWithContext.OnReceive);

            {
                // Thread for Assert.
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var requesterFiber = new PoolFiber(requesterThread, new DefaultExecutor());

                requesterFiber.Pause();
                var response = timeCheck.SendRequest("hello");
                var fiberInPause = new PoolFiber();
                var timeoutTimer = fiberInPause.Schedule(() =>
                {
                    response.Dispose();
                    requesterThread.Queue((_) => Assert.Fail());
                }, 10000);
                response.SetCallbackOnReceive(() => fiberInPause.Enqueue(() =>
                {
                    timeoutTimer.Dispose();
                    requesterFiber.Resume(() =>
                    {
                        bool received = response.TryReceive(out DateTime result);
                        Assert.IsTrue(received);
                        Assert.AreEqual(result, now);
                        requesterThread.Stop();
                    });
                }));
                requesterThread.Run();
            }
        }

        [Test]
        public void SynchronousRequestWithMultipleReplies()
        {
            var countChannel = new RequestReplyChannel<string, int>();
            var allSent = new AutoResetEvent(false);

            {
                IFiber responder = new PoolFiber();
                Action<IRequest<string, int>> onRequest =
                    delegate (IRequest<string, int> req)
                    {
                        for (var i = 0; i <= 5; i++)
                            req.SendReply(i);
                        allSent.Set();
                    };
                var actionWithContext = new ActionWithContext<IRequest<string, int>>(onRequest, responder);
                var subscriber = countChannel.AddResponder(actionWithContext.OnReceive);
            }

            {
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var requesterFiber = new PoolFiber(requesterThread, new DefaultExecutor());
                requesterFiber.Pause();
                var fiberInPause = new PoolFiber();
                var response = countChannel.SendRequest("hello");
                allSent.WaitOne(10000, false);
                using (response)
                {
                    var timeoutTimer1 = fiberInPause.Schedule(() =>
                    {
                        response.Dispose();
                        requesterThread.Queue((_) => Assert.Fail());
                    }, 1000);
                    int i = 0;
                    Action[] receivingArray = new Action[1];
                    Action receiving = () =>
                    {
                        timeoutTimer1.Dispose();
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
                                    var timeoutTimer2 = fiberInPause.Schedule(() =>
                                    {
                                        response.Dispose();
                                        requesterThread.Queue((_) => Assert.Fail());
                                    }, 1000);
                                    response.SetCallbackOnReceive(() => fiberInPause.Enqueue(() =>
                                    {
                                        timeoutTimer2.Dispose();
                                        receivingArray[0].Invoke();
                                    }));
                                }
                                else
                                {
                                    requesterFiber.Pause();
                                    var timeoutTimer2 = fiberInPause.Schedule(() =>
                                    {
                                        response.Dispose();
                                        requesterThread.Queue((_) => Assert.Fail());
                                    }, 3000);
                                    response.SetCallbackOnReceive(() => fiberInPause.Enqueue(() =>
                                    {
                                        timeoutTimer2.Dispose();
                                        requesterFiber.Resume(() =>
                                        {
                                            requesterFiber.Enqueue(() =>
                                            {
                                                received = response.TryReceive(out result);
                                                Assert.IsTrue(received);
                                                Assert.AreEqual(5, result);

                                                requesterFiber.Pause();
                                                var timeoutTimer3 = fiberInPause.Schedule(() =>
                                                {
                                                    response.Dispose();
                                                    requesterThread.Stop();
                                                }, 3000);
                                                response.SetCallbackOnReceive(() => fiberInPause.Enqueue(() =>
                                                {
                                                    requesterThread.Queue((_) => Assert.Fail());
                                                }));
                                            });
                                        });
                                    }));
                                }
                            });
                        });
                    };
                    receivingArray[0] = receiving;
                    response.SetCallbackOnReceive(() => fiberInPause.Enqueue(receiving));
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
            var actionWithContext = new ActionWithContext<IRequest<string, int>>(onRequest, responder);
            var subscriber = countChannel.AddResponder(actionWithContext.OnReceive);

            var requests = new List<string>();
            requests.AddRange(dic.Keys);
            requests.Add("daikon");
            int indexRequest = 0;

            // Thread for Assert.
            var mainThread = new ThreadPoolAdaptorFromQueueForThread();
            var mainFiber = new PoolFiber(mainThread, new DefaultExecutor());

            int timeoutInMs = 500;
            IReply<int> response = null;
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
                    var timeoutTimer = mainFiber.Schedule(() =>
                    {
                        response.Dispose();
                        Assert.Fail();
                    }, timeoutInMs);
                    Action onReceive = () => mainFiber.Enqueue(() =>
                    {
                        timeoutTimer.Dispose();
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
                    response.SetCallbackOnReceive(onReceive);
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
            var actionWithContext = new ActionWithContext<IRequest<string, int>>(onRequest, responder);
            var subscriber = countChannel.AddResponder(actionWithContext.OnReceive);

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
            var workFiber = new PoolFiber();
            var tcs = new TaskCompletionSource<T>();
            var timeoutTimer = workFiber.Schedule(() =>
            {
                if (tcs.TrySetCanceled())
                {
                    reply.Dispose();
                }
            }, timeoutInMs);
            Action onReceive = () => workFiber.Enqueue(() =>
            {
                timeoutTimer.Dispose();
                bool isReceived = reply.TryReceive(out T responseData);
                if (isReceived)
                {
                    tcs.TrySetResult(responseData);
                }
                else
                {
                    tcs.TrySetCanceled();
                }
            });
            reply.SetCallbackOnReceive(onReceive);
            return tcs.Task;
        }
    }
}