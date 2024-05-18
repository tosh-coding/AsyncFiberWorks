using System;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorksTests.Perf;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class TimerActionTests
    {
        [Test, TestCaseSource("TimerFactories")]
        public void CallbackFromTimer(IOneshotTimerFactory timerFactory)
        {
            var stubFiber = new StubFiber();
            long counter = 0;
            Action action = () => { counter++; };
            var timer = timerFactory.Schedule(() => stubFiber.Enqueue(action), 2);

            Thread.Sleep(20);
            stubFiber.ExecuteOnlyPendingNow();
            Thread.Sleep(140);
            stubFiber.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, counter);
        }

        [Test, TestCaseSource("TimerFactories")]
        public void CallbackFromIntervalTimerWithCancel(IIntervalTimerFactory timerFactory)
        {
            var stubFiber = new StubFiber();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var timer = timerFactory.ScheduleOnInterval(() => stubFiber.Enqueue(actionOnTimer), 2, 100);

            Thread.Sleep(20);
            stubFiber.ExecuteOnlyPendingNow();
            Thread.Sleep(140);
            stubFiber.ExecuteOnlyPendingNow();
            timer.Dispose();
            Thread.Sleep(100);
            stubFiber.ExecuteOnlyPendingNow();
            Assert.AreEqual(2, counterOnTimer);
        }

        [Test, TestCaseSource("TimerFactories")]
        public void CallbackFromTimerWithCancel(IOneshotTimerFactory timerFactory)
        {
            var stubFiber = new StubFiber();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var timer = timerFactory.Schedule(() => stubFiber.Enqueue(actionOnTimer), 2);

            timer.Dispose();
            Thread.Sleep(20);
            stubFiber.ExecuteOnlyPendingNow();
            Assert.AreEqual(0, counterOnTimer);
        }

        static object[] TimerFactories =
        {
            new object[] { new ThreadingTimerFactory() },
#if NETFRAMEWORK || WINDOWS
            new object[] { new WaitableTimerExFactory() },
#endif
        };
    }
}
