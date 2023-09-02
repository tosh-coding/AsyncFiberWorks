using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Retlang.Channels;
using Retlang.Fibers;

namespace RetlangTests
{
    [TestFixture]
    public class StubFiberTests
    {
        [Test]
        public void StubFiberPendingTasksShouldAllowEnqueueOfCommandsWhenExecutingAllPending()
        {
            var sut = new StubFiber { ExecutePendingImmediately = false };

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

            sut.ExecuteAllPendingUntilEmpty();
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
            sut.ExecuteAllPending();

            // Both firstInMs have passed.
            Thread.Sleep(200);
            sut.ExecuteAllPending();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(1, scheduleOnIntervalFired);

            // The regularInMs has passed.
            Thread.Sleep(600);
            sut.ExecuteAllPending();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(2, scheduleOnIntervalFired);

            intervalSub.Dispose();

            // The regularInMs has passed after dispose.
            Thread.Sleep(600);
            sut.ExecuteAllPending();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(2, scheduleOnIntervalFired);
        }

        [Test]
        public void ShouldCompletelyClearPendingActionsBeforeExecutingNewActions()
        {
            var msgs = new List<int>();

            var sut = new StubFiber { ExecutePendingImmediately = true };
            var channel = new Channel<int>();
            const int count = 4;

            channel.Subscribe(sut, delegate(int x)
                                         {
                                             if (x == count)
                                             {
                                                 return;
                                             }

                                             channel.Publish(x + 1);
                                             msgs.Add(x);
                                         });

            channel.Publish(0);

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
            sut.ExecuteAllPending();
            channel.Subscribe(sut, x => { });
            channel.Publish(2);

            Assert.AreEqual(1, sut.NumSubscriptions);
            Assert.AreEqual(1, sut.NumScheduledActions);
            Assert.AreEqual(1, sut.Pending.Count);

            sut.Dispose();

            Assert.AreEqual(0, sut.NumSubscriptions);
            Assert.AreEqual(0, sut.NumScheduledActions);
            Assert.AreEqual(0, sut.Pending.Count);
        }
    }
}