﻿using System.Collections.Generic;
using System.Diagnostics;
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
    public class ThreadPoolTests
    {
        static int NumberOfMinThreads = 2;

        [OneTimeSetUp]
        public void Init()
        {
            // Wake up the threads. Assume there are at least two.
            for (int i = 0; i < NumberOfMinThreads; i++)
            {
                ThreadPool.QueueUserWorkItem((_) => Thread.Sleep(600));
                ThreadPool.QueueUserWorkItem((_) => Thread.Sleep(600));
            }
            Thread.Sleep(1300);
        }

        [Test]
        public void EnqueueToDefaultThreadPool()
        {
            EnqueueToThreadPool(new DefaultThreadPool(), NumberOfMinThreads);
        }

        [Test]
        public async Task EnqueueToUserThreadPool()
        {
            using (var pool = UserThreadPool.Create(NumberOfMinThreads))
            {
                pool.Start();
                EnqueueToThreadPool(pool, pool.ThreadList.Length);
                pool.Stop();
                await pool.JoinAsync();
            }
        }

        public void EnqueueToThreadPool(IThreadPool threadPool, int minThreads)
        {
            int loopCount = 100;
            int sleepMs = 15;
            var lockObj = new object();
            var threadIdCounter = new Dictionary<int, int>();

            for (int i = 0; i < loopCount; i++)
            {
                threadPool.Queue((_) =>
                {
                    Thread.Sleep(sleepMs);
                    lock (lockObj)
                    {
                        int counter;
                        if (!threadIdCounter.TryGetValue(Thread.CurrentThread.ManagedThreadId, out counter))
                        {
                            counter = 0;
                        }
                        counter += 1;
                        threadIdCounter[Thread.CurrentThread.ManagedThreadId] = counter;
                    }
                });
            }

            int waitTimeMs = loopCount * sleepMs / minThreads + 500;
            Thread.Sleep(waitTimeMs);

            Assert.GreaterOrEqual(threadIdCounter.Count, 2);
            Assert.AreEqual(loopCount, threadIdCounter.Sum(x => x.Value));
        }

        [Test]
        public async Task JoinUserThreadPool()
        {
            var pool = UserThreadPool.Create();
            pool.Start();
            pool.Enqueue(() =>
            {
                Thread.Sleep(600);
                pool.Stop();
            });
            var sw = Stopwatch.StartNew();
            await pool.JoinAsync();
            var elapsedMs = sw.ElapsedMilliseconds;
            Assert.Greater(elapsedMs, 500);
        }

        [Test]
        public void SetNameOfUserThreadPool()
        {
            var pool1 = UserThreadPool.Create(2, poolName: "AbcPool");
            var pool2 = UserThreadPool.Create(2, poolName: "DefPool");
            Assert.AreEqual("DefPool", pool2.PoolName);
            Assert.AreEqual("AbcPool", pool1.PoolName);
        }

        [Test]
        public void SetNumberOfThreadsOfUserThreadPool()
        {
            var pool1 = UserThreadPool.Create(1);
            var pool2 = UserThreadPool.Create(2);
            var pool4 = UserThreadPool.Create(4);
            var pool13 = UserThreadPool.Create(13);
            Assert.AreEqual(13, pool13.ThreadList.Length);
            Assert.AreEqual(4, pool4.ThreadList.Length);
            Assert.AreEqual(2, pool2.ThreadList.Length);
            Assert.AreEqual(1, pool1.ThreadList.Length);
        }

        [Test]
        public void UseMultipleUserThreadPool()
        {
            int loopCount = 1000;
            long counter1 = 0;
            long counter2 = 0;
            long counter3 = 0;
            using (var pool1 = UserThreadPool.StartNew(1, poolName: "Pool1"))
            using (var pool2 = UserThreadPool.StartNew(2, poolName: "Pool2"))
            using (var pool3 = UserThreadPool.StartNew(3, poolName: "Pool3"))
            {
                for (int i = 0; i < loopCount; i++)
                {
                    pool1.Enqueue(() => Interlocked.Increment(ref counter1));
                    pool2.Enqueue(() => Interlocked.Increment(ref counter2));
                    pool3.Enqueue(() => Interlocked.Increment(ref counter3));
                }

                var sw = Stopwatch.StartNew();
                while (true)
                {
                    if ((Interlocked.Read(ref counter1) == loopCount) &&
                        (Interlocked.Read(ref counter2) == loopCount) &&
                        (Interlocked.Read(ref counter3) == loopCount))
                    {
                        break;
                    }
                    if (sw.ElapsedMilliseconds > 10)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }

                Assert.AreEqual(loopCount, counter1);
                Assert.AreEqual(loopCount, counter2);
                Assert.AreEqual(loopCount, counter3);
            }
        }

        [Test]
        public void PoolFiberWithUserThreadPool()
        {
            using (var pool = UserThreadPool.StartNew(4))
            {
                var fiber1 = new PoolFiber(pool);
                var fiber2 = new PoolFiber(pool);
                var fiber3 = new PoolFiber(pool);
                using (var fiberSubscriptions1 = new Subscriptions())
                using (var fiberSubscriptions2 = new Subscriptions())
                using (var fiberSubscriptions3 = new Subscriptions())
                {
                    int loopCount = 1000;

                    long counter1 = 0;
                    long counter2 = 0;
                    long counter3 = 0;
                    for (int i = 0; i < loopCount; i++)
                    {
                        fiber1.Enqueue(() =>
                        {
                            var tmp = Interlocked.Read(ref counter1);
                            Interlocked.Exchange(ref counter1, tmp + 1);
                        });
                        fiber2.Enqueue(() =>
                        {
                            var tmp = Interlocked.Read(ref counter2);
                            Interlocked.Exchange(ref counter2, tmp + 2);
                        });
                        fiber3.Enqueue(() =>
                        {
                            var tmp = Interlocked.Read(ref counter3);
                            Interlocked.Exchange(ref counter3, tmp + 3);
                        });
                    }
                    Thread.Sleep(1000);

                    Assert.AreEqual(loopCount * 1, counter1);
                    Assert.AreEqual(loopCount * 2, counter2);
                    Assert.AreEqual(loopCount * 3, counter3);
                }
            }
        }
    }
}
