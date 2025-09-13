using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Procedures;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class FiberTaskWaiterTests
    {
        [Test]
        public async Task TestWait()
        {
            var taskList = new FiberAndTaskPairList();
            int resultCounter = 0;
            var lockObj = new object();

            Func<int, Task> func = async (maxCount) =>
            {
                using (var activator = new SequentialTaskWaiter(taskList))
                {
                    int counter = 0;
                    while (counter < maxCount)
                    {
                        await activator.ExecutionStarted();
                        lock (lockObj)
                        {
                            resultCounter += 1;
                        }
                        counter += 1;
                    }
                }
            };

            var task1 = func(3);
            var task2 = func(6);

            var fiber = new PoolFiber();
            for (int i = 0; i < 10; i++)
            {
                await taskList.InvokeSequentialAsync(fiber);
            }

            await Task.WhenAll(task1, task2);
            Assert.AreEqual(3 + 6, resultCounter);
        }

        [Test]
        public async Task TestCancellation()
        {
            var taskList = new FiberAndTaskPairList();
            int resultCounter = 0;
            int exceptionCounter = 0;
            var lockObj = new object();
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var func = new Func<Task>(async () =>
            {
                using (var activator = new SequentialTaskWaiter(taskList, cancellationToken))
                {
                    try
                    {
                        while (true)
                        {
                            await activator.ExecutionStarted();
                            lock (lockObj)
                            {
                                resultCounter += 1;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        exceptionCounter += 1;
                    }
                }
            });

            var task1 = func();

            var fiber = new PoolFiber();
            await taskList.InvokeSequentialAsync(fiber);
            await taskList.InvokeSequentialAsync(fiber);
            await taskList.InvokeSequentialAsync(fiber);
            cts.Cancel();

            await task1;
            Assert.AreEqual(3, resultCounter);
            Assert.AreEqual(1, exceptionCounter);
        }

        [Test]
        public async Task TestWaitingOfT()
        {
            var handlerList = new FiberAndHandlerPairList<int>();
            int resultCounter = 0;
            var lockObj = new object();

            Func<int, Task> func = async (maxCount) =>
            {
                using (var reg = new AsyncRegister<int>(handlerList))
                {
                    int counter = 0;
                    while (counter < maxCount)
                    {
                        var e = await reg.WaitSetting();
                        lock (lockObj)
                        {
                            resultCounter += e.Arg;
                        }
                        counter += 1;
                    }
                }
            };

            var task1 = func(3);
            var task2 = func(6);

            var defaultContext = new PoolFiber();
            for (int i = 0; i < 10; i++)
            {
                await handlerList.PublishSequentialAsync(i + 1, defaultContext);
            }

            await Task.WhenAll(task1, task2);
            Assert.AreEqual(Sigma(3) + Sigma(6), resultCounter);
        }

        int Sigma(int n)
        {
            return n * (n + 1) / 2;
        }

        [Test]
        public async Task TestCancellationOfT()
        {
            var handlerList = new FiberAndHandlerPairList<int>();
            int resultCounter = 0;
            int exceptionCounter = 0;
            int totalCounter = 0;
            var lockObj = new object();
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var func = new Func<Task>(async () =>
            {
                using (var reg = new AsyncRegister<int>(handlerList, cancellationToken))
                {
                    try
                    {
                        while (true)
                        {
                            var ret = await reg.WaitSetting();
                            lock (lockObj)
                            {
                                totalCounter += ret.Arg;
                                resultCounter += 1;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        exceptionCounter += 1;
                    }
                }
            });

            var task1 = func();

            var fiber = new PoolFiber();
            await handlerList.PublishSequentialAsync(1, fiber);
            await handlerList.PublishSequentialAsync(2, fiber);
            await handlerList.PublishSequentialAsync(3, fiber);
            cts.Cancel();

            await task1;
            Assert.AreEqual(3, resultCounter);
            Assert.AreEqual(6, totalCounter);
            Assert.AreEqual(1, exceptionCounter);
        }

        [Test]
        public async Task TestWaitingOfProcessedFlagEventArgs()
        {
            var driver = new FiberAndHandlerPairList<int>();
            int resultCounter = 0;
            var lockObj = new object();

            var cts = new CancellationTokenSource();
            Func<int, Task> func = async (threshold) =>
            {
                using (var reg = new AsyncRegister<int>(driver, cts.Token))
                {
                    while (true)
                    {
                        var e = await reg.WaitSetting();
                        if (e.Arg < threshold)
                        {
                            e.Processed = false;
                        }
                        else
                        {
                            lock (lockObj)
                            {
                                resultCounter += threshold;
                            }
                            e.Processed = true;
                        }
                    }
                }
            };

            var task1 = func(7);
            var task2 = func(5);
            var task3 = func(3);
            var task4 = func(1);

            var defaultContext = new PoolFiber();
            for (int i = 0; i < 10; i++)
            {
                int eventArg = i + 1;
                await driver.PublishSequentialAsync(eventArg, defaultContext);
            }

            cts.Cancel();
            try
            {
                await Task.WhenAll(task1, task2, task3, task4);
            }
            catch (OperationCanceledException)
            {
            }
            await Task.Delay(40);
            int expectedValue = 1 + 1 + 3 + 3 + 5 + 5 + 7 + 7 + 7 + 7;
            Assert.AreEqual(expectedValue, resultCounter);
        }
    }
}
