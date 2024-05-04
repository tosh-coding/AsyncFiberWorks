using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using NUnit.Framework;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class AsyncFiberTests
    {
        [Test]
        public async Task ChainAsyncFiberTest()
        {
            var fiber = new ChainAsyncFiber();
            await AsyncFiberTest(fiber);
        }

        [Test]
        public async Task LoopRunningAsyncFiberTest()
        {
            var fiber = new LoopRunningAsyncFiber();
            await AsyncFiberTest(fiber);
            fiber.Dispose();
        }

        [Test]
        public async Task SemaphoreLoopAsyncFiberTest()
        {
            var fiber = new SemaphoreLoopAsyncFiber();
            await AsyncFiberTest(fiber);
            fiber.Dispose();
        }

        async Task AsyncFiberTest(IAsyncFiber fiber)
        {
            int counter = 0;
            var tcs = new TaskCompletionSource<int>();

            fiber.Enqueue(async () =>
            {
                Assert.AreEqual(0, counter);
                counter = 1;
                await Task.Delay(10).ConfigureAwait(false);
                Assert.AreEqual(1, counter);
                counter = 2;
            });
            fiber.Enqueue(async () =>
            {
                Assert.AreEqual(2, counter);
                counter = 10;
                await Task.Delay(1).ConfigureAwait(false);
                Assert.AreEqual(10, counter);
                counter = 20;
            });
            fiber.Enqueue(async () =>
            {
                Assert.AreEqual(20, counter);
                counter += 100;
                await Task.Yield();
            });
            fiber.Enqueue(async () =>
            {
                tcs.SetResult(0);
            });

            await tcs.Task;
            Assert.AreEqual(120, counter);
        }
    }
}
