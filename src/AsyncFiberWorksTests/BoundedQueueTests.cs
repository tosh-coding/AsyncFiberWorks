using System;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Core;
using Rhino.Mocks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class BoundedQueueTests
    {
        [Test]
        public void NoExceptionHandling()
        {
            var repo = new MockRepository();
            var action = repo.StrictMock<Action>();
            var failure = new Exception();
            action();
            LastCall.Throw(failure);

            repo.ReplayAll();

            var queue = new DefaultQueue();
            queue.Enqueue(action);

            try
            {
                queue.ExecuteNextBatch();
                Assert.Fail("Should throw Exception");
            }
            catch (Exception commFailure)
            {
                Assert.AreSame(failure, commFailure);
            }
            repo.VerifyAll();
        }
        
        [Test]
        public void ShouldOnlyExecuteActionsQueuedWhileNotStopped()
        {
            var mockery = new MockRepository();
            var action1 = mockery.StrictMock<Action>();
            var action2 = mockery.StrictMock<Action>();
            var action3 = mockery.StrictMock<Action>();

            using (mockery.Record())
            {
                action1();
                action2();
            }

            using (mockery.Playback())
            {
                var queue = new DefaultQueue();
                queue.Enqueue(action1);

                var run = new Thread(queue.Run);

                run.Start();
                Thread.Sleep(100);
                queue.Enqueue(action2);
                queue.Stop();
                queue.Enqueue(action3);
                Thread.Sleep(100);
                run.Join();
            }
        }

        [Test]
        public void MaxDepth()
        {
            var queue = new BoundedQueue();
            queue.MaxDepth = 2;
            queue.Enqueue(delegate { });
            queue.Enqueue(delegate { });

            try
            {
                queue.Enqueue(delegate { });
                Assert.Fail("failed");
            }
            catch (QueueFullException failed)
            {
                Assert.AreEqual(2, failed.Depth);
                Assert.AreEqual("Attempted to enqueue item into full queue: 2", failed.Message);
            }
        }
    }
}