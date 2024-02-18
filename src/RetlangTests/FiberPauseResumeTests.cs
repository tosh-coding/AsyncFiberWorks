using NUnit.Framework;
using Retlang.Core;
using Retlang.Fibers;
using System.Threading;
using System.Threading.Tasks;

namespace RetlangTests
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
            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

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
            fiber.Enqueue(() => counter += 1);
            fiber.ExecuteAll();
            Assert.AreEqual(1, counter);

            fiber.Resume(() => counter = 5);
            fiber.ExecuteAll();
            Assert.AreEqual(6, counter);
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
            var taskQuery = SomeWebApiAccessAsync();
            fiber.Enqueue(() => counter += 1);
            fiber.Enqueue(() => counter += 1);
            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            _ = await taskQuery;
            fiber.Resume(() => counter = 5);
            Thread.Sleep(1);
            Assert.AreEqual(8, counter);
        }

        async Task<string> SomeWebApiAccessAsync()
        {
            await Task.Delay(100);
            return @"{""result"":""success""}";
        }
    }
}
