using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Procedures;
using NUnit.Framework;
using System;
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
    }
}
