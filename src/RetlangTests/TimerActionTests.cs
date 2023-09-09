using System;
using System.Threading;
using NUnit.Framework;
using Retlang.Core;
using Retlang.Fibers;

namespace RetlangTests
{
    [TestFixture]
    public class TimerActionTests
    {
        [Test]
        public void Cancel()
        {
            var stubFiber = StubFiber.StartNew();
            var executionCount = 0;
            Action action = () => executionCount++;
            var timer = new TimerAction(action, 1, 2, stubFiber);
            timer.ExecuteOnFiberThread();
            Assert.AreEqual(1, executionCount);
            timer.Dispose();
            timer.ExecuteOnFiberThread();

            Assert.AreEqual(1, executionCount);
        }

        [Test]
        public void CallbackFromTimer()
        {
            var stubFiber = StubFiber.StartNew();
            long counter = 0;
            Action action = () => { counter++; };
            var timer = new TimerAction(action, 2, Timeout.Infinite, stubFiber);
            timer.Start();

            Thread.Sleep(20);
            stubFiber.ExecuteAllPending();
            Thread.Sleep(140);
            stubFiber.ExecuteAllPending();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void CallbackFromIntervalTimerWithCancel()
        {
            var stubFiber = StubFiber.StartNew();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var timer = new TimerAction(actionOnTimer, 2, 100, stubFiber);
            timer.Start();

            Thread.Sleep(20);
            stubFiber.ExecuteAllPending();
            Thread.Sleep(140);
            stubFiber.ExecuteAllPending();
            timer.Dispose();
            Thread.Sleep(100);
            stubFiber.ExecuteAllPending();
            Assert.AreEqual(2, counterOnTimer);
        }

        [Test]
        public void CallbackFromTimerWithCancel()
        {
            var stubFiber = StubFiber.StartNew();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var timer = new TimerAction(actionOnTimer, 2, Timeout.Infinite, stubFiber);
            timer.Start();

            timer.Dispose();
            Thread.Sleep(20);
            stubFiber.ExecuteAllPending();
            Assert.AreEqual(0, counterOnTimer);
        }
    }
}