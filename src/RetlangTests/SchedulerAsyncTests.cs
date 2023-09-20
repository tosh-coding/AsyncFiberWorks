using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Retlang.Core;
using Retlang.Fibers;

namespace RetlangTests
{
    [TestFixture]
    public class SchedulerAsyncTests
    {
        [Test]
        public void CallbackFromTimer()
        {
            var stubFiber = new StubFiberSlim();
            long counter = 0;
            Action action = () => { counter++; };
            var timerTask = stubFiber.ScheduleAsync(action, 2);

            Thread.Sleep(20);
            stubFiber.ExecuteAllPending();
            Thread.Sleep(140);
            stubFiber.ExecuteAllPending();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void CallbackFromIntervalTimerWithCancel()
        {
            var stubFiber = new StubFiberSlim();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var cancellation = new CancellationTokenSource();
            var timerTask = stubFiber.ScheduleOnIntervalAsync(actionOnTimer, 2, 100, cancellation.Token);

            Thread.Sleep(20);
            stubFiber.ExecuteAllPending();
            Thread.Sleep(140);
            stubFiber.ExecuteAllPending();
            cancellation.Cancel();
            Thread.Sleep(100);
            stubFiber.ExecuteAllPending();
            Assert.AreEqual(2, counterOnTimer);
        }

        [Test]
        public void CallbackFromTimerWithCancel()
        {
            var stubFiber = new StubFiberSlim();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var cancellation = new CancellationTokenSource();
            var timerTask = stubFiber.ScheduleAsync(actionOnTimer, 2, cancellation.Token);

            cancellation.Cancel();
            Thread.Sleep(20);
            stubFiber.ExecuteAllPending();
            Assert.AreEqual(0, counterOnTimer);
        }

        [Test]
        public void ResumeInDotNetThreadPool()
        {
            var fiber = ThreadFiberSlim.StartNew();
            ResumeInDotNetThreadPoolAsync(fiber).Wait();
        }

        public async Task ResumeInDotNetThreadPoolAsync(ThreadFiberSlim fiber)
        {
            await fiber.SwitchTo();
            var threadIdFiber = Thread.CurrentThread.ManagedThreadId;
            await DefaultThreadPool.Instance.SwitchTo();

            await fiber.ScheduleAsync(() => { }, 1);
            var threadId1 = Thread.CurrentThread.ManagedThreadId;
            Assert.AreNotEqual(threadIdFiber, threadId1);

            await fiber.ScheduleAsync(() => { }, 2);
            var threadId2 = Thread.CurrentThread.ManagedThreadId;
            Assert.AreNotEqual(threadIdFiber, threadId2);

            await fiber.ScheduleAsync(() => { }, 3);
            var threadId3 = Thread.CurrentThread.ManagedThreadId;
            Assert.AreNotEqual(threadIdFiber, threadId3);

            var cancel4 = new CancellationTokenSource();
            var t4 = fiber.ScheduleOnIntervalAsync(() => { }, 1, 20, cancel4.Token);
            cancel4.Cancel();
            await t4;
            var threadId4 = Thread.CurrentThread.ManagedThreadId;
            Assert.AreNotEqual(threadIdFiber, threadId4);

            var cancel5 = new CancellationTokenSource();
            var t5 = fiber.ScheduleOnIntervalAsync(() => { }, 3, 40, cancel5.Token);
            cancel5.Cancel();
            await t5;
            var threadId5 = Thread.CurrentThread.ManagedThreadId;
            Assert.AreNotEqual(threadIdFiber, threadId5);
        }
    }
}
