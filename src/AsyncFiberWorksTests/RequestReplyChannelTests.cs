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
            var timeCheck = new RequestReplyChannel<string, DateTime>();
            var now = DateTime.Now;

            // Responder.
            {
                var fiber = new PoolFiber();
                Action<IRequest<string, DateTime>> onRequest = (req) => req.SendReply(now);
                var subscriber = timeCheck.AddResponder(fiber.CreateAction(onRequest));
            }

            // Requester.
            {
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var requesterFiber = new PoolFiber(requesterThread, new DefaultExecutor());

                requesterFiber.Pause();

                var fiberInPause = new PoolFiberSlim(new OneShotExecutor());
                var response = timeCheck.SendRequest("hello");
                var timeoutTimer = fiberInPause.Schedule(() =>
                {
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

            // Responder.
            {
                var fiber = new PoolFiber();
                Action<IRequest<string, int>> onRequest =
                    delegate (IRequest<string, int> req)
                    {
                        for (var i = 0; i <= 5; i++)
                            req.SendReply(i);
                        allSent.Set();
                    };
                var subscriber = countChannel.AddResponder(fiber.CreateAction(onRequest));
            }

            // Requester.
            {
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var requesterFiber = new PoolFiber(requesterThread, new DefaultExecutor());
                Action actionAssertFail = () => requesterThread.Queue((_) => Assert.Fail());

                requesterFiber.Pause();

                var fiberInPause = new PoolFiber();
                using (var response = countChannel.SendRequest("hello"))
                {
                    allSent.WaitOne(10000, false);
                    var timeoutTimer = fiberInPause.Schedule(() =>
                    {
                        response.Dispose();
                        actionAssertFail();
                    }, 1000);
                    int i = 0;
                    int state = 0;
                    Action[] actionList = new Action[1];
                    Action action = () => fiberInPause.Enqueue(() =>
                    {
                        if (state == 0)
                        {
                            timeoutTimer?.Dispose();
                            timeoutTimer = null;
                            requesterFiber.Resume(() =>
                            {
                                bool received = response.TryReceive(out int result);
                                Assert.IsTrue(received);
                                Assert.AreEqual(result, i);
                                i += 1;

                                requesterFiber.Enqueue(() =>
                                {
                                    requesterFiber.Pause();
                                    if (i < 5)
                                    {
                                        timeoutTimer?.Dispose();
                                        timeoutTimer = fiberInPause.Schedule(() =>
                                        {
                                            response.Dispose();
                                            actionAssertFail();
                                        }, 1000);
                                        response.SetCallbackOnReceive(actionList[0]);
                                    }
                                    else
                                    {
                                        timeoutTimer?.Dispose();
                                        timeoutTimer = fiberInPause.Schedule(() =>
                                        {
                                            response.Dispose();
                                            actionAssertFail();
                                        }, 3000);
                                        response.SetCallbackOnReceive(actionList[0]);

                                        state = 1;
                                    }
                                });
                            });
                        }
                        else if (state == 1)
                        {
                            timeoutTimer?.Dispose();
                            timeoutTimer = null;
                            requesterFiber.Resume(() =>
                            {
                                requesterFiber.Enqueue(() =>
                                {
                                    bool received = response.TryReceive(out int result);
                                    Assert.IsTrue(received);
                                    Assert.AreEqual(5, result);

                                    requesterFiber.Pause();
                                    timeoutTimer = fiberInPause.Schedule(() =>
                                    {
                                        response.Dispose();
                                        requesterThread.Stop();
                                    }, 3000);
                                    response.SetCallbackOnReceive(actionList[0]);
                                });
                            });
                        }
                        else
                        {
                            actionAssertFail();
                        }
                    });
                    actionList[0] = action;
                    response.SetCallbackOnReceive(action);
                    requesterThread.Run();
                }
            }
        }

        [Test]
        public void ChangeResponseSync()
        {
            var countChannel = new RequestReplyChannel<string, int>();
            var dic = new (string, int)[] {
                ("apple", 100),
                ("banana", 200),
                ("carrot", 300),
            }.ToDictionary(x => x.Item1, x => x.Item2);

            // Responder.
            {
                IFiber responder = new PoolFiber();
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
                var subscriber = countChannel.AddResponder(responder.CreateAction(onRequest));
            }

            // Requester.
            {
                var mainThread = new ThreadPoolAdaptorFromQueueForThread();
                var mainFiber = new PoolFiber(mainThread, new DefaultExecutor());

                var requests = new List<string>();
                requests.AddRange(dic.Keys);
                requests.Add("daikon");
                int indexRequest = 0;

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
        }

        [Test]
        public async Task ChangeResponseAsync()
        {
            var countChannel = new RequestReplyChannel<string, int>();
            var dic = new (string, int)[] {
                ("apple", 100),
                ("banana", 200),
                ("carrot", 300),
            }.ToDictionary(x => x.Item1, x => x.Item2);

            // Responder.
            {
                IFiber responder = new PoolFiber();
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
                var subscriber = countChannel.AddResponder(responder.CreateAction(onRequest));
            }

            // Requester.
            {
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
                            int responseData = await WaitReply(response, 500);
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
        }

        private static Task<T> WaitReply<T>(IReply<T> reply, int timeoutInMs)
        {
            var workFiber = new PoolFiber();
            var tcs = new TaskCompletionSource<T>();
            var timeoutTimer = workFiber.Schedule(() =>
            {
                tcs.TrySetCanceled();
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

        [Test]
        public async Task OnlyFirstOneIsAcquired()
        {
            var countChannel = new RequestReplyChannel<int, int>();
            int resourceHolderId = 0;

            // Responder.
            {
                IFiber responder = new PoolFiber();
                Action<IRequest<int, int>> onRequest =
                    (IRequest<int, int> req) =>
                    {
                        Task.Run(async () =>
                        {
                            await responder.SwitchTo();

                            // There is no resource holder yet.
                            if (resourceHolderId == 0)
                            {
                                resourceHolderId = req.Request;
                                req.SendReply(resourceHolderId);
                            }
                            // There is already a resource holder.
                            else
                            {
                                req.SendReply(resourceHolderId);
                            }
                        });
                    };
                var subscriber = countChannel.AddResponder(responder.CreateAction(onRequest));
            }

            // Requester.
            {
                var response1 = countChannel.SendRequest(1);
                var response2 = countChannel.SendRequest(2);
                var response3 = countChannel.SendRequest(3);
                var response4 = countChannel.SendRequest(4);
                var response5 = countChannel.SendRequest(5);

                try
                {
                    var t1 = WaitReply(response1, 500);
                    var t2 = WaitReply(response2, 500);
                    var t3 = WaitReply(response3, 500);
                    var t4 = WaitReply(response4, 500);
                    var t5 = WaitReply(response5, 500);

                    await Task.WhenAll(t1, t2, t3, t4, t5);

                    // Only one requestor acquires resources.
                    Assert.AreEqual(t1.Result, t2.Result);
                    Assert.AreEqual(t1.Result, t3.Result);
                    Assert.AreEqual(t1.Result, t4.Result);
                    Assert.AreEqual(t1.Result, t5.Result);
                }
                catch (OperationCanceledException)
                {
                    Assert.Fail();
                }
                finally
                {
                    response1.Dispose();
                    response2.Dispose();
                    response3.Dispose();
                    response4.Dispose();
                    response5.Dispose();
                }
            }
        }
    }
}