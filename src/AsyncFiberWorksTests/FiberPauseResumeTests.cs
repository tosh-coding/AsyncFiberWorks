using NUnit.Framework;
using AsyncFiberWorks.Fibers;
using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;

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
            fiber.Enqueue((e) => e.PauseWhileRunning(async () =>
            {
                await tcs.Task;
                await e.EnqueueToOriginThreadAsync(() =>
                {
                    counter = 5;
                });
            }));

            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            // To resume.
            tcs.SetResult(0);

            Thread.Sleep(1);
            Assert.AreEqual(6, counter);
        }

        [Test]
        public void PauseAndResumeConcurrentQueue()
        {
            var queue = new ConcurrentQueueActionQueue();
            var fiber = new PoolFiber(new ThreadPoolAdapter(queue));
            int counter = 0;
            fiber.Enqueue(() => counter += 1);
            queue.ExecuteNextBatch();
            Assert.AreEqual(1, counter);

            var tcs = new TaskCompletionSource<Action>();

            // Pause.
            fiber.Enqueue((e) => e.PauseWhileRunning(async () =>
            {
                var act = await tcs.Task;
                await e.EnqueueToOriginThreadAsync(act);
            }));

            {
                fiber.Enqueue(() =>
                {
                    counter += 1;
                });
                queue.ExecuteNextBatch();
                Assert.AreEqual(1, counter);
            }

            // Resume.
            tcs.SetResult(() => counter = 5);
            Thread.Sleep(10);

            queue.ExecuteNextBatch();
            Thread.Sleep(10);
            queue.ExecuteNextBatch();
            Assert.AreEqual(6, counter);
        }

        [Test]
        public void PauseAndTimerCallback()
        {
            using (var userThreadPool = UserThreadPool.StartNew(1))
            {
                var nonstopFiber = userThreadPool.CreateFiber();
                var pauseFiber = new PoolFiber();

                int counter = 0;
                var reset = new AutoResetEvent(false);

                // Pause.
                pauseFiber.Enqueue((e) => e.PauseWhileRunning(async () =>
                {
                    var tcs = new TaskCompletionSource<int>();
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10);
                        await nonstopFiber.SwitchTo();
                        counter = 10;
                        // Resume.
                        tcs.SetResult(0);
                    });
                    counter += 1;

                    await tcs.Task;
                }));
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
            fiber.Enqueue((e) => e.PauseWhileRunning(
                async () =>
                {
                    var act = await tcs.Task;
                    await e.EnqueueToOriginThreadAsync(act);
                }));

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
        public async Task AnotherThreadPoolFiberTest()
        {
            var fiber = AnotherThreadPool.Instance.CreateFiber();
            var counter = new IntClass();
            counter.Value = 0;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            fiber.Enqueue(() => counter.Value += 3);
            fiber.Enqueue((e) =>
            {
                e.PauseWhileRunning(async () =>
                {
                    // Some kind of asynchronous operation.
                    _ = await SomeWebApiAccessAsync();
                    await e.EnqueueToOriginThreadAsync(() =>
                    {
                        counter.Value = 10;

                        // For the completion of the test.
                        tcs.SetResult(true);
                    });
                });
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
