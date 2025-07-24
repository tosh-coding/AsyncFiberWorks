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
