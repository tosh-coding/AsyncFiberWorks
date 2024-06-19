using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
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
            var driver = new ActionDriver();
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

            var fiber = new PoolFiber();
            for (int i = 0; i < 10; i++)
            {
                fiber.Enqueue((e) => driver.InvokeAsync(e));
                await fiber.EnqueueTaskAsync(() => Task.CompletedTask);
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
                await driver.InvokeAsync(i + 1);
            }

            await Task.WhenAll(task1, task2);
            Assert.AreEqual(Sigma(3) + Sigma(6), resultCounter);
        }

        int Sigma(int n)
        {
            return n * (n + 1) / 2;
        }
    }
}
