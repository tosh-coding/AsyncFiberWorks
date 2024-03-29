﻿using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorksTests
{
    public class PerfExecutor : IExecutor
    {
        public void Execute(List<Action> toExecute)
        {
            foreach (var action in toExecute)
            {
                action();
            }
            if (toExecute.Count < 10000)
            {
                Thread.Sleep(1);
            }
        }

        public void Execute(Action toExecute)
        {
            toExecute();
        }
    }

    public struct MsgStruct
    {
        public int count;
    }

    [TestFixture]
    public class PerfTests
    {
        [Test, Explicit]
        public void PointToPointPerfTestWithStruct()
        {
            RunBoundedQueue();
        }

        [Test, Explicit]
        public void BusyWaitQueuePointToPointPerfTestWithStruct()
        {
            RunBusyWaitQueue();
        }
      
        private static void RunBoundedQueue()
        {
            var executor = new BoundedQueue(new PerfExecutor()) { MaxDepth = 10000, MaxEnqueueWaitTimeInMs = 1000 };
            using (var fiber = new ThreadFiber(executor))
            {
                fiber.Start();
                var channel = new Channel<MsgStruct>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                Action<MsgStruct> onMsg = delegate(MsgStruct count)
                {
                    if (count.count == max)
                    {
                        reset.Set();
                    }
                };
                var subscriptionFiber = fiber.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(fiber, onMsg);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                using (new PerfTimer(max))
                {
                    for (var i = 0; i <= max; i++)
                    {
                        channel.Publish(new MsgStruct { count = i });
                    }
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }

        private static void RunBusyWaitQueue()
        {
            var executor = new BusyWaitQueue(new PerfExecutor(), 100000, 30000);
            using (var fiber = new ThreadFiber(executor))
            {
                fiber.Start();
                var channel = new Channel<MsgStruct>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                Action<MsgStruct> onMsg = delegate(MsgStruct count)
                                              {
                                                  if (count.count == max)
                                                  {
                                                      reset.Set();
                                                  }
                                              };
                var subscriptionFiber = fiber.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(fiber, onMsg);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                using (new PerfTimer(max))
                {
                    for (var i = 0; i <= max; i++)
                    {
                        channel.Publish(new MsgStruct { count = i });
                    }
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }

        [Test, Explicit]
        public void PointToPointPerfTestWithInt()
        {
            var executor = new BoundedQueue(new PerfExecutor()) { MaxDepth = 10000, MaxEnqueueWaitTimeInMs = 1000 };
            using (var fiber = new ThreadFiber(executor))
            {
                fiber.Start();
                var channel = new Channel<int>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                Action<int> onMsg = delegate(int count)
                                        {
                                            if (count == max)
                                            {
                                                reset.Set();
                                            }
                                        };
                var subscriptionFiber = fiber.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(fiber, onMsg);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                using (new PerfTimer(max))
                {
                    for (var i = 0; i <= max; i++)
                    {
                        channel.Publish(i);
                    }
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }

        [Test, Explicit]
        public void PointToPointPerfTestWithObject()
        {
            var executor = new BoundedQueue(new PerfExecutor()) { MaxDepth = 100000, MaxEnqueueWaitTimeInMs = 1000 };
            using (var fiber = new ThreadFiber(executor))
            {
                fiber.Start();
                var channel = new Channel<object>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                var end = new object();
                Action<object> onMsg = delegate(object msg)
                                           {
                                               if (msg == end)
                                               {
                                                   reset.Set();
                                               }
                                           };
                var subscriptionFiber = fiber.BeginSubscription();
                var subscriptionChannel = channel.Subscribe(fiber, onMsg);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                using (new PerfTimer(max))
                {
                    var msg = new object();
                    for (var i = 0; i <= max; i++)
                    {
                        channel.Publish(msg);
                    }
                    channel.Publish(end);
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }
    }
}