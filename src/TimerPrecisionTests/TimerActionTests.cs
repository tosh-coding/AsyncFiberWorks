using AsyncFiberWorks.Fibers;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimerPrecisionTests
{
    [TestFixture]
    public class TimerActionTests
    {
        [Test]
        public void CallbackFromIntervalTimerWithCancel()
        {
            var timer = new IntervalThreadingTimer();
            var fiber = new PoolFiber();
            long counterOnTimer = 0;
            var tickArrived = new AutoResetEvent(false);
            Action actionOnTimer = () => { counterOnTimer++; tickArrived.Set(); };
            var cancellation = new CancellationTokenSource();
            int intervalMs = 300;
            Action waitTickOrFail = () =>
            {
                if (!tickArrived.WaitOne(intervalMs * 2))
                {
                    Assert.Fail("Timeout waiting for tick.");
                }
            };
            Action waitNoTickOrFail = () =>
            {
                if (tickArrived.WaitOne(intervalMs))
                {
                    Assert.Fail("Unexpected tick after cancel.");
                }
            };
            timer.ScheduleOnInterval(() => fiber.Enqueue(actionOnTimer), intervalMs / 2, intervalMs, cancellation.Token);

            waitTickOrFail();
            Assert.AreEqual(1, counterOnTimer);
            waitTickOrFail();
            Assert.AreEqual(2, counterOnTimer);
            waitTickOrFail();
            Assert.AreEqual(3, counterOnTimer);
            waitTickOrFail();
            Assert.AreEqual(4, counterOnTimer);
            waitTickOrFail();
            Assert.AreEqual(5, counterOnTimer);
            waitTickOrFail();
            Assert.AreEqual(6, counterOnTimer);
            cancellation.Cancel();
            waitNoTickOrFail();
            Assert.AreEqual(6, counterOnTimer);
            waitNoTickOrFail();
            Assert.AreEqual(6, counterOnTimer);
            tickArrived.Dispose();
            timer.Dispose();
        }

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
