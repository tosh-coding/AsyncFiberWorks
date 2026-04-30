using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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

            var fibersField = typeof(KeyedPoolFiber)
                .GetField("_fibers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fibers = fibersField.GetValue(fiber) as System.Collections.IDictionary;
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
    }
}
