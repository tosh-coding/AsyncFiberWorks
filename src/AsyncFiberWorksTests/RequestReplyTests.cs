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
    public class RequestReplyTests
    {
        [Test]
        public void SynchronousRequestReply()
        {
            var timeCheck = new Channel<IRequest<string, DateTime>>();
            var now = DateTime.Now;

            // Responder.
            {
                var fiber = new PoolFiber();
                Action<IRequest<string, DateTime>> onRequest = (req) => req.ReplyTo.Publish(now);
                var subscriber = timeCheck.Subscribe(fiber, onRequest);
            }

            // Requester.
            {
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var requesterFiber = new PoolFiber(requesterThread, new DefaultExecutor());

                requesterFiber.Pause();

                var workFiber = new PoolFiberSlim(new OneShotExecutor());
                var timeoutTimer = workFiber.Schedule(() =>
                {
                    requesterThread.Queue((_) => Assert.Fail());
                }, 10000);
                var disposableRequest = new Unsubscriber();
                var responseChannel = new Channel<DateTime>();
                var response = responseChannel.Subscribe(workFiber, (result) =>
                {
                    timeoutTimer.Dispose();
                    disposableRequest.Dispose();
                    requesterFiber.Resume(() =>
                    {
                        Assert.AreEqual(result, now);
                        requesterThread.Stop();
                    });
                });
                timeCheck.Publish(new RequestReplyChannelRequest<string, DateTime>("hello", responseChannel));
                disposableRequest.AppendDisposable(response);
                requesterThread.Run();
            }
        }

        [Test]
        public void MultipleReplies()
        {
            var countChannel = new Channel<IRequest<string, int>>();
            var allSent = new AutoResetEvent(false);

            // Responder.
            {
                var fiber = new PoolFiber();
                Action<IRequest<string, int>> onRequest =
                    delegate (IRequest<string, int> req)
                    {
                        for (var i = 0; i <= 5; i++)
                            req.ReplyTo.Publish(i);
                    };
                var subscriber = countChannel.Subscribe(fiber, onRequest);
            }

            // Requester.
            {
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var workFiber = new PoolFiber(requesterThread, new DefaultExecutor());
                Action actionAssertFail = () => requesterThread.Queue((_) => Assert.Fail());

                var timeoutTimer = workFiber.Schedule(() =>
                {
                    actionAssertFail();
                }, 1000);
                int i = 0;
                int state = 0;
                var responseChannel = new Channel<int>();
                using (var response = responseChannel.Subscribe(workFiber, (result) =>
                {
                    if (state == 0)
                    {
                        int copyI = i;
                        int copyResult = result;
                        requesterThread.Queue((_) => Assert.AreEqual(copyResult, copyI));
                        i += 1;
                        if (i >= 5)
                        {
                            state = 1;
                        }
                    }
                    else if (state == 1)
                    {
                        timeoutTimer?.Dispose();
                        timeoutTimer = null;
                        Assert.AreEqual(5, result);
                        requesterThread.Stop();
                    }
                    else
                    {
                        actionAssertFail();
                    }
                }))
                {
                    countChannel.Publish(new RequestReplyChannelRequest<string, int>("hello", responseChannel));
                    requesterThread.Run();
                }
            }
        }

        [Test]
        public void ChangeResponseSync()
        {
            var countChannel = new Channel<IRequest<string, int>>();
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
                            req.ReplyTo.Publish(value);
                        }
                        else
                        {
                            req.ReplyTo.Publish(-1);
                        }
                    };
                var subscriber = countChannel.Subscribe(responder, onRequest);
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
                var ownAction = new List<Action>();
                Action action = () =>
                {
                    string requestData = requests[indexRequest];
                    indexRequest += 1;

                    if (indexRequest >= requests.Count)
                    {
                        mainFiber.Enqueue(() => { mainThread.Stop(); });
                    }
                    else
                    {
                        var disposables = new Unsubscriber();
                        var timeoutTimer = mainFiber.Schedule(() =>
                        {
                            disposables.Dispose();
                            Assert.Fail();
                        }, timeoutInMs);
                        var responseChannel = new Channel<int>();
                        var response = responseChannel.Subscribe(mainFiber, (responseData) =>
                        {
                            timeoutTimer.Dispose();
                            disposables.Dispose();
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
                        countChannel.Publish(new RequestReplyChannelRequest<string, int>(requestData, responseChannel));
                        disposables.AppendDisposable(response);
                    }
                };
                ownAction.Add(action);

                mainFiber.Enqueue(action);
                mainThread.Run();
                Assert.AreEqual(requests.Count, indexRequest);
            }
        }

        private static Task<int> WaitReply(Channel<IRequest<int, int>> countChannel, int requestData, int timeoutInMs)
        {
            var workFiber = new PoolFiber();
            var disposables = new Unsubscriber();
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var timeoutTimer = workFiber.Schedule(() =>
            {
                tcs.TrySetCanceled();
                disposables.Dispose();
            }, timeoutInMs);
            var responseChannel = new Channel<int>();
            var response = responseChannel.Subscribe(workFiber, (responseData) =>
            {
                timeoutTimer.Dispose();
                tcs.TrySetResult(responseData);
                disposables.Dispose();
            });

            countChannel.Publish(new RequestReplyChannelRequest<int, int>(requestData, responseChannel));
            disposables.AppendDisposable(response);
            return tcs.Task;
        }

        [Test]
        public async Task OnlyFirstOneIsAcquired()
        {
            var countChannel = new Channel<IRequest<int, int>>();
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
                                req.ReplyTo.Publish(resourceHolderId);
                            }
                            // There is already a resource holder.
                            else
                            {
                                req.ReplyTo.Publish(resourceHolderId);
                            }
                        });
                    };
                var subscriber = countChannel.Subscribe(responder, onRequest);
            }

            // Requester.
            {
                try
                {
                    var t1 = WaitReply(countChannel, 1, 500);
                    var t2 = WaitReply(countChannel, 2, 500);
                    var t3 = WaitReply(countChannel, 3, 500);
                    var t4 = WaitReply(countChannel, 4, 500);
                    var t5 = WaitReply(countChannel, 5, 500);

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
            }
        }
    }
}