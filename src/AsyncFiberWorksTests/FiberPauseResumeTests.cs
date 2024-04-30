using NUnit.Framework;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using System.Threading;
using System.Threading.Tasks;
using AsyncFiberWorks.Channels;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class FiberPauseResumeTests
    {
        [Test]
        public void PauseAndResumePoolFiber()
        {
            var fiber = new PoolFiberSlim();
            int counter = 0;
            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            fiber.Pause();
            {
                fiber.Enqueue(() => counter += 1);
                Thread.Sleep(1);
                Assert.AreEqual(1, counter);
            }
            fiber.Resume(() => counter = 5);

            Thread.Sleep(1);
            Assert.AreEqual(6, counter);
        }

        [Test]
        public void PauseAndResumeStubFiber()
        {
            var fiber = new StubFiberSlim();
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
                nonstopFiber.Start();
                var pauseFiber = new PoolFiberSlim();
                
                pauseFiber.Pause();
                var reset = new AutoResetEvent(false);
                int counter = 0;
                nonstopFiber.Schedule(() =>
                {
                    counter = 10;
                    pauseFiber.Resume(() => { });
                    reset.Set();
                }, 10);
                counter += 1;

                reset.WaitOne(10000, false);
                Assert.AreEqual(10, counter);
            }
        }

        [Test]
        public async Task PauseAndAsyncMethod()
        {
            var fiber = new PoolFiberSlim();
            int counter = 0;
            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            fiber.Pause();
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
            fiber.Resume(() => counter = 5);
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
            var fiber = new PoolFiberSlim();
            var counter = new IntClass();
            counter.Value = 0;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            fiber.Enqueue(() => PauseAndTaskFuncInFiber(fiber, counter, tcs));
            fiber.Enqueue(() => counter.Value += 1);
            Thread.Sleep(1);
            Assert.AreEqual(0, counter.Value);

            // For the completion of the test.
            await tcs.Task.ConfigureAwait(false);
            Assert.AreEqual(11, counter.Value);
        }

        void PauseAndTaskFuncInFiber(PoolFiberSlim fiber, IntClass counter, TaskCompletionSource<bool> tcs)
        {
            // Pause and start async method.
            fiber.Pause();
            Task.Run(async () =>
            {
                try
                {
                    // Some kind of asynchronous operation.
                    _ = await SomeWebApiAccessAsync();
                }
                finally
                {
                    // Calls the resume function at the end of an asynchronous method.
                    fiber.Resume(() => counter.Value = 10);
                }
            });

            // For the completion of the test.
            fiber.Enqueue(() => tcs.SetResult(true));
        }

        [Test]
        public async Task EnqueueTaskTest()
        {
            var fiber = new PoolFiberSlim();
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
