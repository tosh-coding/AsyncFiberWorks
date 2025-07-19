using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Procedures;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class AsyncRegisterTests
    {
        [Test]
        public async Task WaitingOfAsyncRegister()
        {
            var taskList = new FiberAndTaskPairList();
            int resultCounter = 0;
            var lockObj = new object();

            Func<int, Task> func = async (maxCount) =>
            {
                var reg = new AsyncRegister(taskList);
                try
                {
                    int counter = 0;
                    while (counter < maxCount)
                    {
                        await reg.WaitSetting();
                        lock (lockObj)
                        {
                            resultCounter += 1;
                        }
                        counter += 1;
                    }
                }
                finally
                {
                    reg.Dispose();
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
        public async Task WaitingOfAsyncRegisterOfT()
        {
            var handlerList = new FiberAndHandlerPairList<int>();
            int resultCounter = 0;
            var lockObj = new object();

            Func<int, Task> func = async (maxCount) =>
            {
                var reg = new AsyncRegister<int>(handlerList);
                try
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
                finally
                {
                    reg.Dispose();
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
        public async Task WaitingOfProcessedFlagEventArgs()
        {
            var driver = new FiberAndHandlerPairList<int>();
            int resultCounter = 0;
            var lockObj = new object();

            var cts = new CancellationTokenSource();
            Func<int, Task> func = async (threshold) =>
            {
                var reg = new AsyncRegister<int>(driver);
                try
                {
                    while (true)
                    {
                        var e = await reg.WaitSetting(cts.Token);
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
                finally
                {
                    reg.Dispose();
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
