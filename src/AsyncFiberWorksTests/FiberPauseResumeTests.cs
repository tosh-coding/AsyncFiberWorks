using NUnit.Framework;
using AsyncFiberWorks.Fibers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class FiberPauseResumeTests
    {
        [Test]
        public void PauseAndResumePoolFiber()
        {
            var fiber = new PoolFiber();
            int counter = 0;
            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            var tcs = new TaskCompletionSource<int>();

            // Queue consumption is paused just before the execution of the enqueued async method.
            // Resume when the async method completes. 
            fiber.Enqueue(async () =>
            {
                await tcs.Task;
                return () => counter = 5;
            });

            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            // To resume.
            tcs.SetResult(0);

            Thread.Sleep(1);
            Assert.AreEqual(6, counter);
        }

        [Test]
        public void PauseAndResumeStubFiber()
        {
            var fiber = new StubFiber();
            int counter = 0;
            fiber.Enqueue(() => counter += 1);
            fiber.ExecuteAll();
            Assert.AreEqual(1, counter);

            fiber.Pause();
            {
                fiber.Enqueue(() => counter += 1);
                fiber.ExecuteAll();
                Assert.AreEqual(1, counter);
            }
            fiber.Resume(() => counter = 5);

            fiber.ExecuteAll();
            Assert.AreEqual(6, counter);
        }

        [Test]
        public void PauseAndTimerCallback()
        {
            using (var nonstopFiber = new ThreadFiber())
            {
                var pauseFiber = new PoolFiber();

                int counter = 0;
                var reset = new AutoResetEvent(false);

                // Pause.
                pauseFiber.Enqueue(async () =>
                {
                    var tcs = new TaskCompletionSource<Action>();
                    nonstopFiber.Schedule(() =>
                    {
                        counter = 10;
                        // Resume.
                        tcs.SetResult(() => { });
                    }, 10);
                    counter += 1;

                    return await tcs.Task;
                });
                pauseFiber.Enqueue(() =>
                {
                    reset.Set();
                });
                reset.WaitOne(10000, false);
                Assert.AreEqual(10, counter);
            }
        }

        [Test]
        public async Task PauseAndAsyncMethod()
        {
            var fiber = new PoolFiber();
            int counter = 0;
            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            var tcs = new TaskCompletionSource<Action>();

            // Pause.
            fiber.Enqueue(async () =>
            {
                return await tcs.Task;
            });

            {
                // Call an async method while the fiber is paused.
                var taskQuery = SomeWebApiAccessAsync();

                // Actions that are enqueued during pause are not executed immediately.
                fiber.Enqueue(() => counter += 1);
                fiber.Enqueue(() => counter += 1);
                fiber.Enqueue(() => counter += 1);
                Thread.Sleep(1);
                Assert.AreEqual(1, counter);

                _ = await taskQuery;
            }

            // Resume.
            tcs.SetResult(() => counter = 5);
            // At this timing, the actions that were previously enqueued will be executed.

            Thread.Sleep(1);
            Assert.AreEqual(8, counter);
        }

        async Task<string> SomeWebApiAccessAsync()
        {
            await Task.Delay(100);
            return @"{""result"":""success""}";
        }

        [Test]
        public async Task PauseAndTaskRun()
        {
            var fiber = new PoolFiber();
            var counter = new IntClass();
            counter.Value = 0;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Pause.
            fiber.Enqueue(async () =>
            {
                try
                {
                    // Some kind of asynchronous operation.
                    _ = await SomeWebApiAccessAsync();
                }
                catch
                {
                }

                // Do something in the original fiber. Useful only for special threads.
                return () => counter.Value = 10;
            });
            fiber.Enqueue(() => counter.Value += 1);
            fiber.Enqueue(() => tcs.SetResult(true));

            await Task.Delay(10).ConfigureAwait(false);
            Assert.AreEqual(0, counter.Value);

            await tcs.Task.ConfigureAwait(false);
            Assert.AreEqual(11, counter.Value);
        }

        [Test]
        public async Task EnqueueTaskTest()
        {
            var fiber = new PoolFiber();
            var counter = new IntClass();
            counter.Value = 0;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            fiber.Enqueue(() => counter.Value += 3);
            fiber.Enqueue(async () =>
            {
                // Some kind of asynchronous operation.
                _ = await SomeWebApiAccessAsync();
                return () =>
                {
                    counter.Value = 10;

                    // For the completion of the test.
                    tcs.SetResult(true);
                };
            });
            fiber.Enqueue(() => counter.Value += 1);
            Thread.Sleep(1);
            Assert.AreEqual(3, counter.Value);

            // For the completion of the test.
            await tcs.Task.ConfigureAwait(false);
            Thread.Sleep(20);
            Assert.AreEqual(11, counter.Value);
        }
    }

    class IntClass
    {
        public int Value;
    }
}
