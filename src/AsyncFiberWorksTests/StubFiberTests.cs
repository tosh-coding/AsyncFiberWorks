using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
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
            var subscriptions = new Subscriptions();
            var sut = new StubFiber();

            var scheduleFired = 0;
            var scheduleOnIntervalFired = 0;

            var disposableTimer = sut.Schedule(() => scheduleFired++, 100);
            var subscriptionFiber = subscriptions.BeginSubscription();
            var intervalSub = sut.ScheduleOnInterval(() => scheduleOnIntervalFired++, 100, 500);
            subscriptionFiber.AppendDisposable(intervalSub);

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

            subscriptionFiber.Dispose();

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

            var unsubscriber = channel.Subscribe(sut, delegate (int x)
            {
                if (x == count)
                {
                    return;
                }

                channel.Publish(x + 1);
                msgs.Add(x);
            });

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
            var subscriptions = new Subscriptions();
            var sut = new StubFiber();
            var channel = new Channel<int>();

            var subscriptionFiber1 = subscriptions.BeginSubscription();
            var disposableTimer = sut.Schedule(() => { }, 1000);
            subscriptionFiber1.AppendDisposable(disposableTimer);
            sut.ExecuteOnlyPendingNow();
            
            var subscriptionFiber2 = subscriptions.BeginSubscription();
            var subscriptionChannel = channel.Subscribe(sut, x => { });
            subscriptionFiber2.AppendDisposable(subscriptionChannel);
            channel.Publish(2);

            Assert.AreEqual(2, subscriptions.NumSubscriptions);

            subscriptions.Dispose();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
        }
    }
}