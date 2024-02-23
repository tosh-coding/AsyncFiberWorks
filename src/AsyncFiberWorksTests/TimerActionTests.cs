using System;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class TimerActionTests
    {
        [Test]
        public void CallbackFromTimer()
        {
            var stubFiber = new StubFiber();
            long counter = 0;
            Action action = () => { counter++; };
            var timer = TimerAction.StartNew(() => stubFiber.Enqueue(action), 2);

            Thread.Sleep(20);
            stubFiber.ExecuteOnlyPendingNow();
            Thread.Sleep(140);
            stubFiber.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void CallbackFromIntervalTimerWithCancel()
        {
            var stubFiber = new StubFiber();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var timer = TimerAction.StartNew(() => stubFiber.Enqueue(actionOnTimer), 2, 100);

            Thread.Sleep(20);
            stubFiber.ExecuteOnlyPendingNow();
            Thread.Sleep(140);
            stubFiber.ExecuteOnlyPendingNow();
            timer.Dispose();
            Thread.Sleep(100);
            stubFiber.ExecuteOnlyPendingNow();
            Assert.AreEqual(2, counterOnTimer);
        }

        [Test]
        public void CallbackFromTimerWithCancel()
        {
            var stubFiber = new StubFiber();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var timer = TimerAction.StartNew(() => stubFiber.Enqueue(actionOnTimer), 2);

            timer.Dispose();
            Thread.Sleep(20);
            stubFiber.ExecuteOnlyPendingNow();
            Assert.AreEqual(0, counterOnTimer);
        }
    }
}