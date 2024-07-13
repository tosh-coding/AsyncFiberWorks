using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.FiberSchedulers;
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
        [Test, TestCaseSource(nameof(OneshotTimers))]
        public void CallbackFromTimer(Func<IOneshotTimer> timerCreator)
        {
            var timer = timerCreator();
            var stubFiber = new StubFiber();
            long counter = 0;
            Action action = () => { counter++; };
            timer.Schedule(stubFiber, action, 2);

            Thread.Sleep(20);
            stubFiber.ExecuteOnlyPendingNow();
            Thread.Sleep(140);
            stubFiber.ExecuteOnlyPendingNow();
            Assert.AreEqual(1, counter);
            timer.Dispose();
        }

        [Test, TestCaseSource(nameof(IntervalTimers))]
        public void CallbackFromIntervalTimerWithCancel(Func<IIntervalTimer> timerCreator)
        {
            var timer = timerCreator();
            var fiber = new PoolFiber();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var cancellation = new CancellationTokenSource();
            int intervalMs = 300;
            timer.ScheduleOnInterval(() => fiber.Enqueue(actionOnTimer), intervalMs / 2, intervalMs, cancellation.Token);

            Thread.Sleep(intervalMs);
            Assert.AreEqual(1, counterOnTimer);
            Thread.Sleep(intervalMs);
            Assert.AreEqual(2, counterOnTimer);
            Thread.Sleep(intervalMs);
            Assert.AreEqual(3, counterOnTimer);
            Thread.Sleep(intervalMs);
            Assert.AreEqual(4, counterOnTimer);
            Thread.Sleep(intervalMs);
            Assert.AreEqual(5, counterOnTimer);
            Thread.Sleep(intervalMs);
            Assert.AreEqual(6, counterOnTimer);
            cancellation.Cancel();
            Thread.Sleep(intervalMs);
            Assert.AreEqual(6, counterOnTimer);
            Thread.Sleep(intervalMs);
            Assert.AreEqual(6, counterOnTimer);
            timer.Dispose();
        }

        [Test, TestCaseSource(nameof(OneshotTimers))]
        public void CallbackFromTimerWithCancel(Func<IOneshotTimer> timerCreator)
        {
            var timer = timerCreator();
            var stubFiber = new StubFiber();
            long counterOnTimer = 0;
            Action actionOnTimer = () => { counterOnTimer++; };
            var cancellation = new CancellationTokenSource();
            timer.Schedule(stubFiber, actionOnTimer, 2, cancellation.Token);

            cancellation.Cancel();
            Thread.Sleep(20);
            stubFiber.ExecuteOnlyPendingNow();
            Assert.AreEqual(0, counterOnTimer);
            timer.Dispose();
        }

        [Test, TestCaseSource(nameof(OneshotTimers))]
        public async Task OneshotTimerDelayTest(Func<IOneshotTimer> timerCreator)
        {
            var timer = timerCreator();
            int tolerance = 32;
            var sw = new Stopwatch();

            // Warm up.
            await timer.ScheduleAsync(1);
            sw.Restart();

            int expectedWaitTime = 300;
            sw.Restart();
            await timer.ScheduleAsync(expectedWaitTime);
            var elapsed = sw.Elapsed;
            Assert.IsTrue(elapsed.TotalMilliseconds > (expectedWaitTime - tolerance));
            Assert.IsTrue(elapsed.TotalMilliseconds < (expectedWaitTime + tolerance));

            expectedWaitTime = 200;
            sw.Restart();
            await timer.ScheduleAsync(expectedWaitTime);
            elapsed = sw.Elapsed;
            Assert.IsTrue(elapsed.TotalMilliseconds > (expectedWaitTime - tolerance));
            Assert.IsTrue(elapsed.TotalMilliseconds < (expectedWaitTime + tolerance));

            expectedWaitTime = 300;
            sw.Restart();
            await timer.ScheduleAsync(expectedWaitTime);
            elapsed = sw.Elapsed;
            Assert.IsTrue(elapsed.TotalMilliseconds > (expectedWaitTime - tolerance));
            Assert.IsTrue(elapsed.TotalMilliseconds < (expectedWaitTime + tolerance));

            timer.Dispose();
        }

        [Test, TestCaseSource(nameof(OneshotTimers))]
        public async Task DelayedSwitchToTest(Func<IOneshotTimer> timerCreator)
        {
            var timer = timerCreator();
            var fiber = new PoolFiber();
            var sw = new Stopwatch();
            int tolerance = 32;

            // warm up.
            sw.Restart();
            await fiber.DelayedSwitchTo(1, timer);

            int expectedWaitTime = 300;
            sw.Restart();
            await fiber.DelayedSwitchTo(expectedWaitTime, timer);
            var elapsed = sw.Elapsed;
            Assert.IsTrue(elapsed.TotalMilliseconds > (expectedWaitTime - tolerance ));
            Assert.IsTrue(elapsed.TotalMilliseconds < (expectedWaitTime + tolerance ), $"elapsedMs={elapsed.TotalMilliseconds}");

            expectedWaitTime = 200;
            sw.Restart();
            await fiber.DelayedSwitchTo(expectedWaitTime, timer);
            elapsed = sw.Elapsed;
            Assert.IsTrue(elapsed.TotalMilliseconds > (expectedWaitTime - tolerance ));
            Assert.IsTrue(elapsed.TotalMilliseconds < (expectedWaitTime + tolerance ));

            expectedWaitTime = 100;
            sw.Restart();
            await fiber.DelayedSwitchTo(expectedWaitTime, timer);
            elapsed = sw.Elapsed;
            Assert.IsTrue(elapsed.TotalMilliseconds > (expectedWaitTime - tolerance ));
            Assert.IsTrue(elapsed.TotalMilliseconds < (expectedWaitTime + tolerance ));

            timer.Dispose();
        }

        static object[] OneshotTimers =
        {
            new object[] { (Func<IOneshotTimer>)(() => new OneshotThreadingTimer()) },
#if NETFRAMEWORK || WINDOWS
            new object[] { (Func<IOneshotTimer>)(() => new OneshotWaitableTimerEx()) },
#endif
        };

        static object[] IntervalTimers =
        {
            new object[] { (Func<IIntervalTimer>)(() => new IntervalThreadingTimer()) },
#if NETFRAMEWORK || WINDOWS
            new object[] { (Func<IIntervalTimer>)(() => new IntervalWaitableTimerEx()) },
#endif
        };

#if NETFRAMEWORK || WINDOWS
        [Test]
        public async Task CancelWaitableTimer()
        {
            var tcs = new TaskCompletionSource<int>();
            var timer = new WaitableTimerEx();
            timer.Set(TimeSpan.FromMilliseconds(300));
            var tmpHandle = ThreadPool.RegisterWaitForSingleObject(timer, (state, timeout) =>
            {
                tcs.SetResult(0);
            }, null, Timeout.Infinite, executeOnlyOnce: true);
            await Task.Delay(100).ConfigureAwait(false);
            bool isCancelled = timer.Cancel();
            Assert.IsTrue(isCancelled);
            await Task.Delay(500).ConfigureAwait(false);
            Assert.IsFalse(tcs.Task.IsCompleted);
            tmpHandle.Unregister(null);
            timer.Dispose();
        }
#endif
    }
}
