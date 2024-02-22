﻿using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class StubFiberTests
    {
        [Test]
        public void StubFiberPendingTasksShouldAllowEnqueueOfCommandsWhenExecutingAllPending()
        {
            var sut = new StubFiber();

            var fired1 = new object();
            var fired2 = new object();
            var fired3 = new object();

            var actionMarkers = new List<object>();

            Action command1 = delegate
                                  {
                                      actionMarkers.Add(fired1);
                                      sut.Enqueue(() => actionMarkers.Add(fired3));
                                  };

            Action command2 = () => actionMarkers.Add(fired2);

            sut.Enqueue(command1);
            sut.Enqueue(command2);

            sut.ExecuteAll();
            Assert.AreEqual(new[] { fired1, fired2, fired3 }, actionMarkers.ToArray());
        }

        [Test]
        public void ScheduledTasksShouldBeExecutedOnceScheduleIntervalShouldBeExecutedEveryTimeExecuteScheduleAllIsCalled()
        {
            var sut = new StubFiber();

            var scheduleFired = 0;
            var scheduleOnIntervalFired = 0;

            sut.Schedule(() => scheduleFired++, 100);
            var intervalSub = sut.ScheduleOnInterval(() => scheduleOnIntervalFired++, 100, 500);

            // add to the pending list.
            Thread.Sleep(200);

            // Both firstInMs have passed.
            Thread.Sleep(300);
            sut.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(1, scheduleOnIntervalFired);

            // The regularInMs has passed.
            Thread.Sleep(400);
            sut.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(2, scheduleOnIntervalFired);

            intervalSub.Dispose();

            // The regularInMs has passed after dispose.
            Thread.Sleep(500);
            sut.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(2, scheduleOnIntervalFired);
        }

        [Test]
        public void ShouldCompletelyClearPendingActionsBeforeExecutingNewActions()
        {
            var msgs = new List<int>();

            var sut = new StubFiber();
            var channel = new Channel<int>();
            const int count = 4;

            var subscriber = new ChannelSubscription<int>(sut, delegate (int x)
            {
                if (x == count)
                {
                    return;
                }

                channel.Publish(x + 1);
                msgs.Add(x);
            });
            channel.Subscribe(subscriber);

            channel.Publish(0);
            sut.ExecuteAll();

            Assert.AreEqual(count, msgs.Count);
            for (var i = 0; i < msgs.Count; i++)
            {
                Assert.AreEqual(i, msgs[i]);
            }
        }

        [Test]
        public void DisposeShouldClearAllLists()
        {
            var sut = new StubFiber();
            var channel = new Channel<int>();

            sut.Schedule(() => { }, 1000);
            sut.ExecuteOnlyPendingNow();
            var subscriber = new ChannelSubscription<int>(sut, x => { });
            channel.Subscribe(subscriber);
            channel.Publish(2);

            Assert.AreEqual(2, sut.NumSubscriptions);

            sut.Dispose();

            Assert.AreEqual(0, sut.NumSubscriptions);
        }
    }
}