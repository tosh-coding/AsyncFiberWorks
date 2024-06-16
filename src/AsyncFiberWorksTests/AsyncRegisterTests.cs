using AsyncFiberWorks.Core;
using AsyncFiberWorks.MessageFilters;
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
            var driver = new AsyncActionDriver();
            int resultCounter = 0;
            var lockObj = new object();

            Func<int, Task> func = async (maxCount) =>
            {
                var reg = new AsyncRegister(driver);
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

            for (int i = 0; i < 10; i++)
            {
                await driver.Invoke();
            }

            await Task.WhenAll(task1, task2);
            Assert.AreEqual(3 + 6, resultCounter);
        }

        [Test]
        public async Task WaitingOfAsyncRegisterOfT()
        {
            var driver = new AsyncMessageDriver<int>();
            int resultCounter = 0;
            var lockObj = new object();

            Func<int, Task> func = async (maxCount) =>
            {
                var reg = new AsyncRegister<int>(driver);
                try
                {
                    int counter = 0;
                    while (counter < maxCount)
                    {
                        var value = await reg.WaitSetting();
                        lock (lockObj)
                        {
                            resultCounter += value;
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

            for (int i = 0; i < 10; i++)
            {
                await driver.Invoke(i + 1);
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
            var driver = new AsyncProcessedFlagMessageDriver<int>();
            int resultCounter = 0;
            var lockObj = new object();

            var cts = new CancellationTokenSource();
            Func<int, Task> func = async (threshold) =>
            {
                var reg = new AsyncRegister<ProcessedFlagEventArgs<int>>(driver);
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

            var eventArgs = new ProcessedFlagEventArgs<int>();
            for (int i = 0; i < 10; i++)
            {
                eventArgs.Arg = i + 1;
                await driver.Invoke(eventArgs);
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
