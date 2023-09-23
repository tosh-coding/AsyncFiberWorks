using NUnit.Framework;
using Retlang.Channels;
using Retlang.Core;
using Retlang.Fibers;
using System.Threading;

namespace RetlangTests
{
    [TestFixture]
    public class FallbackFiberTest
    {
        [Test]
        public void FallbackTest()
        {
            IFiber fallbackFiber;
            int originalManagedThreadId;
            {
                var threadFiber = ThreadFiber.StartNew();
                originalManagedThreadId = threadFiber.Thread.ManagedThreadId;
                fallbackFiber = new FallbackFiber(threadFiber, threadFiber);
            }

            int fiberManagedThreadId1 = 0;
            int fiberManagedThreadId3 = 0;

            int counter = 0;
            fallbackFiber.Enqueue(() =>
            {
                counter += 1;
                fiberManagedThreadId1 = Thread.CurrentThread.ManagedThreadId;
            });

            fallbackFiber.Dispose();

            fallbackFiber.Enqueue(() =>
            {
                counter += 1;
            });
            Thread.Sleep(20);

            fallbackFiber.Enqueue(() =>
            {
                counter += 1;
                fiberManagedThreadId3 = Thread.CurrentThread.ManagedThreadId;
            });
            Thread.Sleep(20);

            Assert.AreEqual(3, counter);
            Assert.AreEqual(originalManagedThreadId, fiberManagedThreadId1);
            Assert.AreNotEqual(originalManagedThreadId, fiberManagedThreadId3);
        }

        [Test]
        public void DisposeUserThreadPoolViaFiberTest()
        {
            var userThreadPool = UserThreadPool.StartNew(1);
            IFiber fallbackFiber;
            int originalManagedThreadId;
            {
                var poolFiber = PoolFiber.StartNew(userThreadPool, new DefaultExecutor());
                originalManagedThreadId = userThreadPool.ThreadList[0].ManagedThreadId;
                var unsubscriber = new Unsubscriber((_) =>
                {
                    poolFiber.Dispose();
                    userThreadPool.Dispose();
                });
                fallbackFiber = new FallbackFiber(poolFiber, unsubscriber);
            }

            int fiberManagedThreadId1 = 0;
            int fiberManagedThreadId3 = 0;

            int counter = 0;
            fallbackFiber.Enqueue(() =>
            {
                counter += 1;
                fiberManagedThreadId1 = Thread.CurrentThread.ManagedThreadId;
            });

            fallbackFiber.Dispose();

            fallbackFiber.Enqueue(() =>
            {
                counter += 1;
            });
            Thread.Sleep(20);

            fallbackFiber.Enqueue(() =>
            {
                counter += 1;
                fiberManagedThreadId3 = Thread.CurrentThread.ManagedThreadId;
            });
            Thread.Sleep(20);

            Assert.AreEqual(3, counter);
            Assert.AreEqual(originalManagedThreadId, fiberManagedThreadId1);
            Assert.AreNotEqual(originalManagedThreadId, fiberManagedThreadId3);
            Assert.AreEqual(ThreadState.Stopped, userThreadPool.ThreadList[0].ThreadState);
        }
    }
}
