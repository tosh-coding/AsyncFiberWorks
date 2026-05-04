using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class KeyedPoolFiberTests
    {
        [Test]
        public void TestSameKeyOrderGuarantee()
        {
            var fiber = new KeyedPoolFiber();
            var results = new List<int>();
            var done = new CountdownEvent(3);

            fiber.EnqueueKeyed(1, () => { results.Add(1); done.Signal(); });
            fiber.EnqueueKeyed(1, () => { results.Add(2); done.Signal(); });
            fiber.EnqueueKeyed(1, () => { results.Add(3); done.Signal(); });

            done.Wait(TimeSpan.FromSeconds(5));
            Assert.AreEqual("1,2,3", string.Join(",", results));
        }


        [Test]
        public void TestDifferentKeysParallelExecution()
        {
            var fiber = new KeyedPoolFiber();
            var done = new CountdownEvent(2);
            var barrier = new Barrier(2);

            fiber.EnqueueKeyed(1, () =>
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(5));
                done.Signal();
            });
            fiber.EnqueueKeyed(2, () =>
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(5));
                done.Signal();
            });

            var result = done.Wait(TimeSpan.FromSeconds(5));
            Assert.IsTrue(result);
        }


        [Test]
        public void TestEntryAutoDelete()
        {
            var fiber = new KeyedPoolFiber();
            var done = new ManualResetEventSlim(false);

            fiber.EnqueueKeyed(1, () => done.Set());
            done.Wait(TimeSpan.FromSeconds(5));

            Thread.Sleep(500);

            var fibers = GetFibersDictionary(fiber);
            Assert.AreEqual(0, fibers.Count);
        }

        [Test]
        public void TestCreateFiber()
        {
            var fiber = new KeyedPoolFiber();
            var done = new ManualResetEventSlim(false);

            var channel = fiber.CreateFiber(1);
            channel.Enqueue(() => { done.Set(); });

            var result = done.Wait(TimeSpan.FromSeconds(5));
            Assert.IsTrue(result);
        }

        [Test]
        public void EnqueueKeyed_WithPause_ShouldNotRunNextActionUntilResume()
        {
            var queue = new ConcurrentQueueActionQueue();
            var threadPool = new ThreadPoolAdapter(queue);
            var keyedFiber = new KeyedPoolFiber(threadPool);

            var executed = new List<int>();
            IFiberExecutionEventArgs capturedEventArgs = null;

            keyedFiber.EnqueueKeyed(1, (e) =>
            {
                capturedEventArgs = e;
                executed.Add(1);
                e.Pause();
            });

            queue.ExecuteNextBatch();
            Assert.AreEqual(1, executed.Count);
            Assert.NotNull(capturedEventArgs);

            keyedFiber.EnqueueKeyed(1, () =>
            {
                executed.Add(2);
            });

            queue.ExecuteNextBatch();

            Assert.AreEqual(1, executed.Count);

            capturedEventArgs.Resume();
            queue.ExecuteNextBatch();

            Assert.AreEqual(2, executed.Count);
            Assert.AreEqual(1, executed[0]);
            Assert.AreEqual(2, executed[1]);
        }

        [Test]
        public void TestEntryCachedWhenCacheEnabled()
        {
            var fiber = new KeyedPoolFiber(cacheCount: 1);
            var done = new ManualResetEventSlim(false);

            fiber.EnqueueKeyed(1, () => done.Set());
            Assert.IsTrue(done.Wait(TimeSpan.FromSeconds(5)));

            Thread.Sleep(200);

            var fibers = GetFibersDictionary(fiber);
            Assert.AreEqual(0, fibers.Count);

            var cache = GetCachedEntriesCollection(fiber);
            Assert.AreEqual(1, cache.Count);
        }

        [Test]
        public void TestCachedEntryIsReusedForAnotherKey()
        {
            var queue = new ConcurrentQueueActionQueue();
            var threadPool = new ThreadPoolAdapter(queue);
            var keyedFiber = new KeyedPoolFiber(threadPool, cacheCount: 1);

            IFiberExecutionEventArgs capturedEventArgs = null;

            keyedFiber.EnqueueKeyed(1, (e) =>
            {
                capturedEventArgs = e;
                e.Pause();
            });
            queue.ExecuteNextBatch();

            var entryForKey1 = GetFiberEntryForKey(keyedFiber, 1);
            var fiberInstance1 = GetPoolFiberFromEntry(entryForKey1);
            Assert.NotNull(fiberInstance1);
            Assert.NotNull(capturedEventArgs);

            capturedEventArgs.Resume();
            queue.ExecuteNextBatch();

            keyedFiber.EnqueueKeyed(2, () => { });

            var entryForKey2 = GetFiberEntryForKey(keyedFiber, 2);
            var fiberInstance2 = GetPoolFiberFromEntry(entryForKey2);

            Assert.AreSame(fiberInstance1, fiberInstance2);
        }

        private static IDictionary GetFibersDictionary(KeyedPoolFiber fiber)
        {
            var fibersField = typeof(KeyedPoolFiber).GetField("_fibers", BindingFlags.NonPublic | BindingFlags.Instance);
            return fibersField.GetValue(fiber) as IDictionary;
        }

        private static ICollection GetCachedEntriesCollection(KeyedPoolFiber fiber)
        {
            var cacheField = typeof(KeyedPoolFiber).GetField("_cachedFiberEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            return cacheField.GetValue(fiber) as ICollection;
        }

        private static object GetFiberEntryForKey(KeyedPoolFiber fiber, int key)
        {
            var fibers = GetFibersDictionary(fiber);
            return fibers[key];
        }

        private static object GetPoolFiberFromEntry(object fiberEntry)
        {
            var fiberField = fiberEntry.GetType().GetField("Fiber", BindingFlags.Public | BindingFlags.Instance);
            return fiberField.GetValue(fiberEntry);
        }
    }
}
