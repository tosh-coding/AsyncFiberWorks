using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Windows.Timer;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

        [Test, TestCaseSource("TimerFactories")]
        public async Task OneshotTimerDelayTest(IOneshotTimerFactory timerFactory)
        {
            var delayFactory = timerFactory;

            await delayFactory.Delay(10);
            await delayFactory.Delay(10);
            await delayFactory.Delay(10);

            var sw = Stopwatch.StartNew();
            await delayFactory.Delay(100);
            var elapsed = sw.Elapsed;

            int diff = 16;
            Assert.IsTrue(elapsed.TotalMilliseconds > (100 - diff));
            Assert.IsTrue(elapsed.TotalMilliseconds < (100 + diff));
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
