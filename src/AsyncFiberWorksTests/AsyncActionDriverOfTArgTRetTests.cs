using NUnit.Framework;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Procedures;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class AsyncActionDriverOfTArgTRetTests
    {
        [Test]
        public async Task DefaultOrder()
        {
            var channel = new AsyncActionDriver<int, bool>(new DefaultAsyncExecutorOfTArgTRet<int>());

            long counter = 0;

            var fiber1 = new PoolFiber();
            Func<int, Task<bool>> receiverFunc1 = async (int msg) =>
            {
                await fiber1.SwitchTo();
                Assert.AreEqual(123, msg);
                await Task.Delay(100);
                counter = 300;
                return default;
            };

            var fiber2 = new PoolFiber();
            Func<int, Task<bool>> receiverFunc2 = async (int msg) =>
            {
                await fiber2.SwitchTo();
                Assert.AreEqual(123, msg);
                counter += 1;
                return default;
            };

            var disposable1 = channel.Subscribe(receiverFunc1);
            var disposable2 = channel.Subscribe(receiverFunc2);

            await channel.Invoke(123);
            Thread.Sleep(50);
            Assert.AreEqual(301, counter);
        }

        [Test]
        public async Task ReverseOrder()
        {
            var channel = new AsyncActionDriver<int, bool>(new ReverseOrderAsyncExecutorOfTArgTRet<int>());

            long counter = 0;

            var fiber1 = new PoolFiber();
            Func<int, Task<bool>> receiverFunc1 = async (int msg) =>
            {
                await fiber1.SwitchTo();
                Assert.AreEqual(123, msg);
                await Task.Delay(100);
                counter = 300;
                return default;
            };

            var fiber2 = new PoolFiber();
            Func<int, Task<bool>> receiverFunc2 = async (int msg) =>
            {
                await fiber2.SwitchTo();
                Assert.AreEqual(123, msg);
                counter += 1;
                return default;
            };

            var disposable1 = channel.Subscribe(receiverFunc1);
            var disposable2 = channel.Subscribe(receiverFunc2);

            await channel.Invoke(123).ConfigureAwait(false);
            Thread.Sleep(50);
            Assert.AreEqual(300, counter);
        }

        [Test]
        public async Task DiscontinuedDuringInvoking()
        {
            var channel = new AsyncActionDriver<int, bool>(new DefaultAsyncExecutorOfTArgTRet<int>());

            long counter = 0;

            Func<int, Task<bool>> receiverFunc1 = async (int msg) =>
            {
                counter = 300;
                return true;
            };

            Func<int, Task<bool>> receiverFunc2 = async (int msg) =>
            {
                counter += 1;
                return false;
            };

            var disposable1 = channel.Subscribe(receiverFunc1);
            var disposable2 = channel.Subscribe(receiverFunc2);

            await channel.Invoke(123);
            Thread.Sleep(50);
            Assert.AreEqual(300, counter);
        }
    }
}
