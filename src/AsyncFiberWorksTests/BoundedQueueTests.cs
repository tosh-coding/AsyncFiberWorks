using System;
using System.Threading;
using NUnit.Framework;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;
using Rhino.Mocks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class BoundedQueueTests
    {
        [Test]
        public void ExceptionHandling()
        {
            var failure = new Exception();
            var handler = new RecordingExceptionHandler();
            var queue = new DefaultQueue(NoneHookOfBatch.Instance, handler);

            queue.Enqueue(() => { throw failure; });

            var executed = queue.ExecuteNextBatch();

            Assert.IsTrue(executed);
            Assert.AreEqual(1, handler.Count);
            Assert.AreSame(failure, handler.LastException);
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

                var run = new Thread(() =>
                {
                    while (queue.ExecuteNextBatch()) { }
                });

                run.Start();
                Thread.Sleep(100);
                queue.Enqueue(action2);
                Thread.Sleep(100);
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

        private class RecordingExceptionHandler : IActionExceptionHandler
        {
            public int Count { get; private set; }
            public Exception LastException { get; private set; }

            public void Handle(Exception exception)
            {
                Count++;
                LastException = exception;
            }
        }
    }
}