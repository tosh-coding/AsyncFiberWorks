using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Timers;
using AsyncFiberWorks.Threading;
using NUnit.Framework;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class FiberAsyncContextTests
    {
        public class MyDataClass
        {
            public static IEnumerable AllFibers
            {
                get
                {
                    Func<IFiber> poolFiber = () => new PoolFiber();
                    yield return new TestCaseData(poolFiber);

                    Func<IFiber> userThreadPoolFiber = () =>
                    {
                        var userThreadPool = UserThreadPool.StartNew();
                        return userThreadPool.CreateFiber();
                    };
                    yield return new TestCaseData(userThreadPoolFiber);

                    Func<IFiber> anotherThreadPoolFiber = () => AnotherThreadPool.Instance.CreateFiber();
                    yield return new TestCaseData(anotherThreadPoolFiber);
                }
            }
        }

        [Test]
        [TestCaseSource(typeof(MyDataClass), nameof(MyDataClass.AllFibers))]
        public async Task EnqueueTest(Func<IFiber> fiberCreator)
        {
            var fiber = fiberCreator();
            int counter = 0;
            var tcs = new TaskCompletionSource<int>();

            fiber.EnqueueTask(async () =>
            {
                Assert.AreEqual(0, counter);
                counter = 1;
                await Task.Delay(10).ConfigureAwait(false);
                Assert.AreEqual(1, counter);
                counter = 2;
            });
            fiber.EnqueueTask(async () =>
            {
                Assert.AreEqual(2, counter);
                counter = 10;
                await Task.Delay(1).ConfigureAwait(false);
                Assert.AreEqual(10, counter);
                counter = 20;
            });
            fiber.EnqueueTask(async () =>
            {
                Assert.AreEqual(20, counter);
                counter += 100;
                await Task.Yield();
            });
            fiber.EnqueueTask(() =>
            {
                tcs.SetResult(0);
                return Task.CompletedTask;
            });

            await tcs.Task;
            Assert.AreEqual(120, counter);
        }

        [Test]
        [TestCaseSource(typeof(MyDataClass), nameof(MyDataClass.AllFibers))]
        public async Task RepeatingTimer(Func<IFiber> fiberCreator)
        {
            var fiber = fiberCreator();
            var timer = new IntervalThreadingTimer();

            var sw = Stopwatch.StartNew();
            int counter = 0;
            timer.ScheduleOnInterval(fiber, async () =>
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
            timer.Dispose();
        }

        [Test]
        [TestCaseSource(typeof(MyDataClass), nameof(MyDataClass.AllFibers))]
        public async Task EnqueueTaskAsyncTest(Func<IFiber> fiberCreator)
        {
            var fiber = fiberCreator();
            int counter = 0;
            var sw = Stopwatch.StartNew();
            var t1 = fiber.EnqueueTaskAsync(async () =>
            {
                counter = 1;
                await Task.Delay(300).ConfigureAwait(false);
            });
            await Task.Delay(10).ConfigureAwait(false);
            fiber.EnqueueTask(() =>
            {
                counter = 2;
                return Task.CompletedTask;
            });
            Assert.AreEqual(1, counter);
            await t1;
            Assert.GreaterOrEqual(sw.Elapsed, TimeSpan.FromMilliseconds(300));
            await Task.Delay(10).ConfigureAwait(false);
            Assert.AreEqual(2, counter);
        }

        [Test]
        [TestCaseSource(typeof(MyDataClass), nameof(MyDataClass.AllFibers))]
        public async Task EnqueueAsyncTest(Func<IFiber> fiberCreator)
        {
            var fiber = fiberCreator();
            int counter = 0;
            await fiber.EnqueueAsync(() =>
            {
                Thread.Sleep(300);
                counter += 1;
            });
            fiber.Enqueue(() =>
            {
                counter *= 100;
            });
            await fiber.EnqueueAsync(() => { });
            Assert.AreEqual(100, counter);
        }
    }
}
