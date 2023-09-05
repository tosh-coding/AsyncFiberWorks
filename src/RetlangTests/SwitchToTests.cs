using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Retlang.Core;
using Retlang.Fibers;

namespace RetlangTests
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
        public void SwitchToFiberSlim()
        {
            var stubFiber = StubFiberSlim.StartNew();
            var ctsStubFiberExecution = new CancellationTokenSource();
            var t = SwitchToFiberSlimAsync(stubFiber, ctsStubFiberExecution);
            try
            {
                stubFiber.ExecuteUntilCanceled(ctsStubFiberExecution.Token);
            }
            catch (OperationCanceledException)
            {
            }
            t.Wait();
        }

        public async Task SwitchToFiberSlimAsync(StubFiberSlim stubFiber, CancellationTokenSource ctsStubFiberExecution)
        {
            await Task.Yield();

            var defaultThreadPool = new DefaultThreadPool();
            var userThreadPoolA = UserThreadPool.StartNew();
            var userThreadPoolB = UserThreadPool.StartNew();

            var threadFiber = ThreadFiberSlim.StartNew();
            var dotnetPoolFiber1 = PoolFiberSlim.StartNew(defaultThreadPool, new DefaultExecutor());
            var dotnetPoolFiber2 = PoolFiberSlim.StartNew();
            var userPoolFiberA1 = PoolFiberSlim.StartNew(userThreadPoolA, new DefaultExecutor());
            var userPoolFiberA2 = PoolFiberSlim.StartNew(userThreadPoolA, new DefaultExecutor());
            var userPoolFiberB1 = PoolFiberSlim.StartNew(userThreadPoolB, new DefaultExecutor());
            var userPoolFiberB2 = PoolFiberSlim.StartNew(userThreadPoolB, new DefaultExecutor());

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
                await stubFiber.SwitchTo();
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

            // Stop the StubFiber.
            ctsStubFiberExecution.Cancel();
        }

        [Test]
        public void SwitchToFiber()
        {
            var stubFiber = new StubFiber();
            stubFiber.Start();
            var ctsStubFiberExecution = new CancellationTokenSource();
            var t = SwitchToFiberAsync(stubFiber, ctsStubFiberExecution);
            try
            {
                stubFiber.ExecuteUntilCanceled(ctsStubFiberExecution.Token);
            }
            catch (OperationCanceledException)
            {
            }
            t.Wait();
        }

        public async Task SwitchToFiberAsync(StubFiber stubFiber, CancellationTokenSource ctsStubFiberExecution)
        {
            await Task.Yield();

            var defaultThreadPool = new DefaultThreadPool();
            var userThreadPoolA = UserThreadPool.StartNew();
            var userThreadPoolB = UserThreadPool.StartNew();

            var threadFiber = new ThreadFiber();
            threadFiber.Start();
            var dotnetPoolFiber1 = new PoolFiber(defaultThreadPool, new DefaultExecutor());
            dotnetPoolFiber1.Start();
            var dotnetPoolFiber2 = new PoolFiber();
            dotnetPoolFiber2.Start();
            var userPoolFiberA1 = new PoolFiber(userThreadPoolA, new DefaultExecutor());
            userPoolFiberA1.Start();
            var userPoolFiberA2 = new PoolFiber(userThreadPoolA, new DefaultExecutor());
            userPoolFiberA2.Start();
            var userPoolFiberB1 = new PoolFiber(userThreadPoolB, new DefaultExecutor());
            userPoolFiberB1.Start();
            var userPoolFiberB2 = new PoolFiber(userThreadPoolB, new DefaultExecutor());
            userPoolFiberB2.Start();

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
                await stubFiber.SwitchTo();
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

            // Stop the StubFiber.
            ctsStubFiberExecution.Cancel();
        }
    }
}
