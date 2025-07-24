using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Timers;
using AsyncFiberWorks.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class ConcurrentQueueActionQueueTests
    {
        [Test]
        public void PendingTasksShouldAllowEnqueueOfCommandsWhenExecutingAllPending()
        {
            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));

            var fired1 = new object();
            var fired2 = new object();
            var fired3 = new object();

            var actionMarkers = new List<object>();

            Action command1 = delegate
            {
                actionMarkers.Add(fired1);
                fiber.Enqueue(() => actionMarkers.Add(fired3));
            };

            Action command2 = () => actionMarkers.Add(fired2);

            fiber.Enqueue(command1);
            fiber.Enqueue(command2);

            queue.ExecuteAll();
            Assert.AreEqual(new[] { fired1, fired2, fired3 }, actionMarkers.ToArray());
        }

        [Test]
        public void ScheduledTasksShouldBeExecutedOnceScheduleIntervalShouldBeExecutedEveryTimeExecuteScheduleAllIsCalled()
        {
            var subscriptions = new Subscriptions();
            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));
            var timer2 = new IntervalThreadingTimer();

            var scheduleFired = 0;
            var scheduleOnIntervalFired = 0;

            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                await fiber.SwitchTo();
                scheduleFired++;
            });
            var subscriptionFiber = subscriptions.BeginSubscription();
            var cancellation = new CancellationTokenSource();
            timer2.ScheduleOnInterval(fiber, () => scheduleOnIntervalFired++, 100, 500, cancellation.Token);
            subscriptionFiber.AppendDisposable(cancellation);

            // add to the pending list.
            Thread.Sleep(200);

            // Both firstInMs have passed.
            Thread.Sleep(300);
            queue.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(1, scheduleOnIntervalFired);

            // The regularInMs has passed.
            Thread.Sleep(400);
            queue.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(2, scheduleOnIntervalFired);

            subscriptionFiber.Dispose();
            timer2.Dispose();

            // The regularInMs has passed after dispose.
            Thread.Sleep(500);
            queue.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, scheduleFired);
            Assert.AreEqual(2, scheduleOnIntervalFired);
        }

        [Test]
        public void ShouldCompletelyClearPendingActionsBeforeExecutingNewActions()
        {
            var msgs = new List<int>();

            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));
            var channel = new Channel<int>();
            const int count = 4;

            var unsubscriber = channel.Subscribe(fiber, delegate (int x)
            {
                if (x == count)
                {
                    return;
                }

                channel.Publish(x + 1);
                msgs.Add(x);
            });

            channel.Publish(0);
            queue.ExecuteAll();

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
            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));
            var channel = new Channel<int>();

            var subscriptionFiber1 = subscriptions.BeginSubscription();
            var cancellation = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000, cancellation.Token);
                await fiber.SwitchTo();
            });
            subscriptionFiber1.AppendDisposable(cancellation);
            queue.ExecuteOnlyPendingNow();
            
            var subscriptionFiber2 = subscriptions.BeginSubscription();
            var subscriptionChannel = channel.Subscribe(fiber, x => { });
            subscriptionFiber2.AppendDisposable(subscriptionChannel);
            channel.Publish(2);

            Assert.AreEqual(2, subscriptions.NumSubscriptions);

            subscriptions.Dispose();

            Assert.AreEqual(0, subscriptions.NumSubscriptions);
        }
    }
}