﻿using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class SnapshotTest
    {
        [Test]
        public void Snapshot()
        {
            using (var fiberReply = new PoolFiber())
            {
                var updatesChannel = new Channel<int>();
                var requestChannel = new Channel<IRequest<Channel<int>, IDisposable>>();
                var lockerResponseValue = new object();

                // A value managed by the responder.
                int currentValue = 0;

                // Set up responder. 
                var subscriptionFiber = fiberReply.BeginSubscription();
                var subscriptionChannel = requestChannel.Subscribe(
                    fiberReply.CreateAction<IRequest<Channel<int>, IDisposable>>(request =>
                    {
                        int value;
                        lock (lockerResponseValue)
                        {
                            value = currentValue;
                        }
                        request.Request.Publish(value);
                        var disposableOfReceiver = updatesChannel.Subscribe((msg) =>
                        {
                            request.Request.Publish(msg);
                        });
                        request.ReplyTo.Publish(disposableOfReceiver);
                    }));
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                Assert.AreEqual(1, requestChannel.NumSubscribers);

                // Start changing values.

                lock (lockerResponseValue)
                {
                    currentValue = 1;
                    updatesChannel.Publish(currentValue);
                }
                lock (lockerResponseValue)
                {
                    currentValue = 2;
                    updatesChannel.Publish(currentValue);
                }

                // Start requesting.
                var requesterThread = new ThreadPoolAdaptorFromQueueForThread();
                var fiberRequest = new PoolFiber(requesterThread, new DefaultExecutor());
                var receivedValues = new List<int>();
                var timeoutTimerCancellation = new Unsubscriber();
                var receiveChannel = new Channel<int>();
                var disposableReceive = receiveChannel.Subscribe((v) =>
                {
                    fiberRequest.Enqueue(() =>
                    {
                        receivedValues.Add(v);
                        Console.WriteLine("Received: " + v);
                    });
                });
                IDisposable handleReceiveReply = null;
                bool handleReceiveDisposed = false;
                IDisposable handleReceiveDisposableOfReceiver = null;
                var replyChannel = new Channel<IDisposable>();
                handleReceiveReply = replyChannel.Subscribe((disposableOfReceiver) => fiberRequest.Enqueue(() =>
                {
                    if (handleReceiveDisposed)
                    {
                        disposableOfReceiver.Dispose();
                        return;
                    }

                    handleReceiveReply.Dispose();
                    handleReceiveReply = null;
                    handleReceiveDisposableOfReceiver = disposableOfReceiver;
                    fiberRequest.Enqueue(() =>
                    {
                        timeoutTimerCancellation.Dispose();
                        lock (lockerResponseValue)
                        {
                            currentValue = 4;
                            updatesChannel.Publish(currentValue);
                        }
                        lock (lockerResponseValue)
                        {
                            currentValue = 8;
                            updatesChannel.Publish(currentValue);
                        }

                        fiberRequest.Schedule(() =>
                        {
                            // Finish.

                            int[] expectedReceiveValues = new int[]
                            {
                                2, 4, 8,
                            };

                            Assert.AreEqual(expectedReceiveValues.Length, receivedValues.Count);

                            for (int i = 0; i < expectedReceiveValues.Length; i++)
                            {
                                Assert.AreEqual(expectedReceiveValues[i], receivedValues[i]);
                            }

                            requesterThread.Stop();
                        }, 200);
                    });
                }));
                requestChannel.Publish(new RequestReplyChannelRequest<Channel<int>, IDisposable>(receiveChannel, replyChannel));
                var timeoutTimer = fiberRequest.Schedule(() =>
                {
                    if (!handleReceiveDisposed)
                    {
                        handleReceiveDisposed = true;
                        if (handleReceiveReply != null)
                        {
                            handleReceiveReply.Dispose();
                            handleReceiveReply = null;
                        }
                        if (handleReceiveDisposableOfReceiver != null)
                        {
                            handleReceiveDisposableOfReceiver.Dispose();
                            handleReceiveDisposableOfReceiver = null;
                        }
                    }
                    Assert.Fail("SnapshotRequestControlEvent.Timeout");
                }, 5000);
                timeoutTimerCancellation.AppendDisposable(timeoutTimer);

                requesterThread.Run();
                if (!handleReceiveDisposed)
                {
                    handleReceiveDisposed = true;
                    if (handleReceiveReply != null)
                    {
                        handleReceiveReply.Dispose();
                        handleReceiveReply = null;
                    }
                    if (handleReceiveDisposableOfReceiver != null)
                    {
                        handleReceiveDisposableOfReceiver.Dispose();
                        handleReceiveDisposableOfReceiver = null;
                    }
                }
                timeoutTimerCancellation.Dispose();
                disposableReceive.Dispose();
            }
        }
    }
}