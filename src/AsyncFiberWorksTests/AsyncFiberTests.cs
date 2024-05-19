using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class AsyncFiberTests
    {
        [Test]
        public async Task ChainAsyncFiberTest()
        {
            var fiber = new ChainAsyncFiber();
            await AsyncFiberTest(fiber);
        }

        async Task AsyncFiberTest(IAsyncFiber fiber)
        {
            int counter = 0;
            var tcs = new TaskCompletionSource<int>();

            fiber.Enqueue(async () =>
            {
                Assert.AreEqual(0, counter);
                counter = 1;
                await Task.Delay(10).ConfigureAwait(false);
                Assert.AreEqual(1, counter);
                counter = 2;
            });
            fiber.Enqueue(async () =>
            {
                Assert.AreEqual(2, counter);
                counter = 10;
                await Task.Delay(1).ConfigureAwait(false);
                Assert.AreEqual(10, counter);
                counter = 20;
            });
            fiber.Enqueue(async () =>
            {
                Assert.AreEqual(20, counter);
                counter += 100;
                await Task.Yield();
            });
            fiber.Enqueue(async () =>
            {
                tcs.SetResult(0);
            });

            await tcs.Task;
            Assert.AreEqual(120, counter);
        }

        [Test]
        public async Task OneshotTimerTest()
        {
            var fiber = new ChainAsyncFiber();

            int counter = 0;
            var timer = fiber.Schedule(async () =>
            {
                await Task.Yield();
                counter += 1;
            }, 5);
            Assert.AreEqual(0, counter);
            await Task.Delay(20).ConfigureAwait(false);
            Assert.AreEqual(1, counter);
            timer.Dispose();
        }

        [Test]
        public async Task OneshotTimerCancallationTest()
        {
            var fiber = new ChainAsyncFiber();

            int counter = 0;
            var timer = fiber.Schedule(async () =>
            {
                await Task.Yield();
                counter += 1;
            }, 50);
            Assert.AreEqual(0, counter);
            timer.Dispose();
            await Task.Delay(100).ConfigureAwait(false);
            Assert.AreEqual(0, counter);
        }

        [Test]
        public async Task RepeatingTimer()
        {
            var fiber = new ChainAsyncFiber();

            var sw = Stopwatch.StartNew();
            int counter = 0;
            var timer = fiber.ScheduleOnInterval(async () =>
            {
                await Task.Yield();
                counter += 1;
                Console.WriteLine($"cb{sw.Elapsed}");
            }, 250, 500);
            Console.WriteLine($"as{sw.Elapsed}");
            Assert.AreEqual(0, counter);
            await Task.Delay(500).ConfigureAwait(false);
            Console.WriteLine($"as{sw.Elapsed}");
            Assert.AreEqual(1, counter);
            await Task.Delay(500).ConfigureAwait(false);
            Console.WriteLine($"as{sw.Elapsed}");
            Assert.AreEqual(2, counter);
            await Task.Delay(500).ConfigureAwait(false);
            Console.WriteLine($"as{sw.Elapsed}");
            Assert.AreEqual(3, counter);
            timer.Dispose();
            await Task.Delay(500).ConfigureAwait(false);
            Console.WriteLine($"as{sw.Elapsed}");
            Assert.AreEqual(3, counter);
        }
    }
}
