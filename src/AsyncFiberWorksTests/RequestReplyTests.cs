using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class RequestReplyTests
    {
        [Test]
        public async Task SimpleRequestReply()
        {
            Func<string, Task<string>> getGreeting;
            var database = new (string, string)[]
            {
                ("morning", "Good morning"),
                ("afternoon", "Hello"),
                ("evening", "Good evening"),
            };
            var keyword = "afternoon";
            var correct_answer = "Hello";

            // Responder.
            {
                var responderFiber = new PoolFiber();
                getGreeting = async (req) =>
                {
                    string response = default;
                    await responderFiber.EnqueueAsync(() =>
                    {
                        response = database.First(x => x.Item1 == req).Item2;
                    });
                    return response;
                };
            }

            // Request.
            var result = await getGreeting(keyword).ConfigureAwait(false);
            Assert.AreEqual(result, correct_answer);
        }

        [Test]
        public async Task MultipleReplies()
        {
            Func<IPublisher<int>, Task> requestMultipleReply;

            // Responder.
            {
                var responderFiber = new PoolFiber();
                requestMultipleReply = async (replyTo) =>
                {
                    await responderFiber.EnqueueAsync(() =>
                    {
                        for (var i = 0; i <= 5; i++)
                        {
                            replyTo.Publish(i);
                        }
                    });
                };
            }

            // Request.
            {
                int i = 0;
                int state = 0;
                var workFiber = new PoolFiber();
                var responseChannel = new Channel<int>();
                IDisposable disposable = default;
                var tcs = new TaskCompletionSource<bool>();
                try
                {
                    disposable = responseChannel.Subscribe(workFiber, (result) =>
                    {
                        try
                        {
                            if (state == 0)
                            {
                                int copyI = i;
                                int copyResult = result;
                                Assert.AreEqual(copyResult, copyI);
                                i += 1;
                                if (i >= 5)
                                {
                                    state = 1;
                                }
                            }
                            else if (state == 1)
                            {
                                Assert.AreEqual(5, result);
                                tcs.SetResult(true);
                            }
                            else
                            {
                                Assert.Fail();
                            }

                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    });
                    await requestMultipleReply(responseChannel);

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        tcs.SetCanceled();
                    });
                    await tcs.Task;
                }
                finally
                {
                    disposable.Dispose();
                }
            }
        }

        private static async Task<int> WaitReply(Func<int, Task<int>> acquireResource, int requestData)
        {
            return await Task.Run(async () =>
            {
                return await acquireResource(requestData).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Test]
        public async Task OnlyFirstOneIsAcquired()
        {
            Func<int, Task<int>> acquireResource;
            int resourceHolderId = 0;

            // Responder.
            {
                var responderFiber = new PoolFiber();
                acquireResource = async (req) =>
                {
                    await responderFiber.SwitchTo();

                    // There is no resource holder yet.
                    if (resourceHolderId == 0)
                    {
                        resourceHolderId = req;
                        return resourceHolderId;
                    }
                    // There is already a resource holder.
                    else
                    {
                        return resourceHolderId;
                    }
                };
            }

            // Requester.
            {
                var t1 = WaitReply(acquireResource, 1);
                var t2 = WaitReply(acquireResource, 2);
                var t3 = WaitReply(acquireResource, 3);
                var t4 = WaitReply(acquireResource, 4);
                var t5 = WaitReply(acquireResource, 5);

                await Task.WhenAll(t1, t2, t3, t4, t5);

                // Only one requestor acquires resources.
                Assert.AreNotEqual(t1.Result, 0);
                Assert.AreEqual(t1.Result, t2.Result);
                Assert.AreEqual(t1.Result, t3.Result);
                Assert.AreEqual(t1.Result, t4.Result);
                Assert.AreEqual(t1.Result, t5.Result);
                Console.WriteLine($"Result: {t1.Result}");
            }
        }
    }
}