using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Procedures;
using AsyncFiberWorks.Threading;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class FiberAndTaskPairListTests
    {
        [Test]
        public void SubscribeSimpleActions()
        {
            // Fiber of main thread for Assertion.
            var mainLoop = new ThreadPoolAdapter();
            var fiber = mainLoop.CreateFiber();

            var taskList = new FiberAndTaskPairList();
            long counter = 0;

            Action action1 = () =>
            {
                Assert.AreEqual(0, counter);
                counter = 300;
            };

            Action action2 = () =>
            {
                Assert.AreEqual(300, counter);
                counter += 1;
            };

            var disposable1 = taskList.Add(action1);
            var disposable2 = taskList.Add(action2);

            _ = Task.Run(async () =>
            {
                await taskList.InvokeSequentialAsync(fiber);
                await fiber.EnqueueAsync(() =>
                {
                    Assert.AreEqual(301, counter);
                });
                mainLoop.Stop();
            });
            mainLoop.Run();
        }

        [Test]
        public void SubscribeFuncTask()
        {
            // Fiber of main thread for Assertion.
            var mainLoop = new ThreadPoolAdapter();
            var fiber = mainLoop.CreateFiber();

            var taskList = new FiberAndTaskPairList();
            long counter = 0;

            Func<Task> action1 = async () =>
            {
                Assert.AreEqual(0, counter);
                await Task.Delay(100).ConfigureAwait(false);
                counter = 300;
            };

            Func<Task> action2 = async () =>
            {
                await Task.Yield();
                Assert.AreEqual(300, counter);
                counter += 1;
            };

            var disposable1 = taskList.Add(action1);
            var disposable2 = taskList.Add(action2);

            _ = Task.Run(async () =>
            {
                await taskList.InvokeSequentialAsync(fiber);
                await fiber.EnqueueAsync(() =>
                {
                    Assert.AreEqual(301, counter);
                });
                mainLoop.Stop();
            });
            mainLoop.Run();
        }

        [Test]
        public async Task AsyncInvokingWithArgument()
        {
            var handlerList = new FiberAndHandlerPairList<int>();

            long counter = 0;

            Func<int, Task> action1 = async (value) =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                counter += value;
            };

            Func<int, Task> action2 = async (value) =>
            {
                await Task.Yield();
                counter += value / 10;
            };

            var disposable1 = handlerList.Add(action1);
            var disposable2 = handlerList.Add(action2);

            var defaultFiber = new PoolFiber();
            await handlerList.PublishSequentialAsync(200, defaultFiber);
            Assert.AreEqual(200 + 20, counter);
            await handlerList.PublishSequentialAsync(10, defaultFiber);
            Assert.AreEqual(200 + 20 + 10 + 1, counter);
        }

        [Test]
        public void ToggleAtUnsubscribe()
        {
            // Fiber of main thread for Assertion.
            var mainLoop = new ThreadPoolAdapter();
            var fiber = mainLoop.CreateFiber();

            var taskList = new FiberAndTaskPairList();
            long counter = 0;
            var unsubscriber = new Unsubscriber();

            Action action = () =>
            {
                counter += 1;
                unsubscriber.Dispose();
            };

            var disposable1 = taskList.Add(action);
            unsubscriber.AppendDisposable(disposable1);
            var disposable2 = taskList.Add(action);
            unsubscriber.AppendDisposable(disposable2);

            _ = Task.Run(async () =>
            {
                await taskList.InvokeSequentialAsync(fiber);
                await fiber.EnqueueAsync(() =>
                {
                    Assert.AreEqual(1, counter);
                });
                mainLoop.Stop();
            });
            mainLoop.Run();
        }
    }
}
