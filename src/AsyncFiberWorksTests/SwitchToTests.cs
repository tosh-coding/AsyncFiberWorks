using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class SwitchToTests
    {
        [Test]
        public void SwitchToThreadPool()
        {
            SwitchToThreadPoolAsync().Wait();
        }

        public async Task SwitchToThreadPoolAsync()
        {
            await Task.Yield();

            var defaultThreadPool = new DefaultThreadPool();
            var userThreadPoolA = UserThreadPool.StartNew();
            var userThreadPoolB = UserThreadPool.StartNew();

            var idListOfDefault = new HashSet<int>();
            var idListOfUserA = new HashSet<int>();
            var idListOfUserB = new HashSet<int>();
            for (int i = 0; i < 500; i++)
            {
                await defaultThreadPool.SwitchTo();
                idListOfDefault.Add(Thread.CurrentThread.ManagedThreadId);

                await userThreadPoolA.SwitchTo();
                idListOfUserA.Add(Thread.CurrentThread.ManagedThreadId);

                await userThreadPoolB.SwitchTo();
                idListOfUserB.Add(Thread.CurrentThread.ManagedThreadId);
            }
            await Task.Yield();

            Assert.AreEqual(0, idListOfDefault.Intersect(idListOfUserA).Count());
            Assert.AreEqual(0, idListOfDefault.Intersect(idListOfUserB).Count());
            Assert.AreEqual(0, idListOfUserA.Intersect(idListOfUserB).Count());
        }

        [Test]
        public void SwitchToFiber()
        {
            var mainThread = new ThreadPoolAdaptor();
            var t = SwitchToFiberAsync(mainThread);
            try
            {
                mainThread.Run();
            }
            catch (OperationCanceledException)
            {
            }
            t.Wait();
        }

        public async Task SwitchToFiberAsync(ThreadPoolAdaptor mainThread)
        {
            await Task.Yield();

            var mainFiber = new PoolFiberSlim(mainThread, new DefaultExecutor());

            var defaultThreadPool = new DefaultThreadPool();
            var userThreadPoolA = UserThreadPool.StartNew();
            var userThreadPoolB = UserThreadPool.StartNew();

            var threadFiber = new ThreadFiber();
            threadFiber.Start();
            var dotnetPoolFiber1 = new PoolFiberSlim(defaultThreadPool, new DefaultExecutor());
            var dotnetPoolFiber2 = new PoolFiberSlim();
            var userPoolFiberA1 = new PoolFiberSlim(userThreadPoolA, new DefaultExecutor());
            var userPoolFiberA2 = new PoolFiberSlim(userThreadPoolA, new DefaultExecutor());
            var userPoolFiberB1 = new PoolFiberSlim(userThreadPoolB, new DefaultExecutor());
            var userPoolFiberB2 = new PoolFiberSlim(userThreadPoolB, new DefaultExecutor());

            var idListOfStub = new HashSet<int>();
            var idListOfThread = new HashSet<int>();
            var idListOfDotnetPool1 = new HashSet<int>();
            var idListOfDotnetPool2 = new HashSet<int>();
            var idListOfUserPoolA1 = new HashSet<int>();
            var idListOfUserPoolA2 = new HashSet<int>();
            var idListOfUserPoolB1 = new HashSet<int>();
            var idListOfUserPoolB2 = new HashSet<int>();
            for (int i = 0; i < 1000; i++)
            {
                await mainFiber.SwitchTo();
                idListOfStub.Add(Thread.CurrentThread.ManagedThreadId);

                await threadFiber.SwitchTo();
                idListOfThread.Add(Thread.CurrentThread.ManagedThreadId);

                await dotnetPoolFiber1.SwitchTo();
                idListOfDotnetPool1.Add(Thread.CurrentThread.ManagedThreadId);

                await dotnetPoolFiber2.SwitchTo();
                idListOfDotnetPool2.Add(Thread.CurrentThread.ManagedThreadId);

                await userPoolFiberA1.SwitchTo();
                idListOfUserPoolA1.Add(Thread.CurrentThread.ManagedThreadId);

                await userPoolFiberA2.SwitchTo();
                idListOfUserPoolA2.Add(Thread.CurrentThread.ManagedThreadId);

                await userPoolFiberB1.SwitchTo();
                idListOfUserPoolB1.Add(Thread.CurrentThread.ManagedThreadId);

                await userPoolFiberB2.SwitchTo();
                idListOfUserPoolB2.Add(Thread.CurrentThread.ManagedThreadId);
            }
            await Task.Yield();

            Assert.AreEqual(0, idListOfStub.Intersect(idListOfThread).Count());
            Assert.AreEqual(0, idListOfStub.Intersect(idListOfDotnetPool1).Count());
            Assert.AreEqual(0, idListOfStub.Intersect(idListOfDotnetPool2).Count());
            Assert.AreEqual(0, idListOfStub.Intersect(idListOfUserPoolA1).Count());
            Assert.AreEqual(0, idListOfStub.Intersect(idListOfUserPoolA2).Count());
            Assert.AreEqual(0, idListOfStub.Intersect(idListOfUserPoolB1).Count());
            Assert.AreEqual(0, idListOfStub.Intersect(idListOfUserPoolB2).Count());

            Assert.AreEqual(0, idListOfThread.Intersect(idListOfDotnetPool1).Count());
            Assert.AreEqual(0, idListOfThread.Intersect(idListOfDotnetPool2).Count());
            Assert.AreEqual(0, idListOfThread.Intersect(idListOfUserPoolA1).Count());
            Assert.AreEqual(0, idListOfThread.Intersect(idListOfUserPoolA2).Count());
            Assert.AreEqual(0, idListOfThread.Intersect(idListOfUserPoolB1).Count());
            Assert.AreEqual(0, idListOfThread.Intersect(idListOfUserPoolB2).Count());

            Assert.Greater(idListOfDotnetPool1.Intersect(idListOfDotnetPool2).Count(), 0);
            Assert.AreEqual(0, idListOfDotnetPool1.Intersect(idListOfUserPoolA1).Count());
            Assert.AreEqual(0, idListOfDotnetPool1.Intersect(idListOfUserPoolA2).Count());
            Assert.AreEqual(0, idListOfDotnetPool1.Intersect(idListOfUserPoolB1).Count());
            Assert.AreEqual(0, idListOfDotnetPool1.Intersect(idListOfUserPoolB2).Count());

            Assert.AreEqual(0, idListOfDotnetPool2.Intersect(idListOfUserPoolA1).Count());
            Assert.AreEqual(0, idListOfDotnetPool2.Intersect(idListOfUserPoolA2).Count());
            Assert.AreEqual(0, idListOfDotnetPool2.Intersect(idListOfUserPoolB1).Count());
            Assert.AreEqual(0, idListOfDotnetPool2.Intersect(idListOfUserPoolB2).Count());

            Assert.Greater(idListOfUserPoolA1.Intersect(idListOfUserPoolA2).Count(), 0);
            Assert.AreEqual(0, idListOfUserPoolA1.Intersect(idListOfUserPoolB1).Count());
            Assert.AreEqual(0, idListOfUserPoolA1.Intersect(idListOfUserPoolB2).Count());

            Assert.AreEqual(0, idListOfUserPoolA2.Intersect(idListOfUserPoolB1).Count());
            Assert.AreEqual(0, idListOfUserPoolA2.Intersect(idListOfUserPoolB2).Count());

            Assert.Greater(idListOfUserPoolB1.Intersect(idListOfUserPoolB2).Count(), 0);

            // Stop the consumer thread.
            mainThread.Stop();
        }
    }
}
