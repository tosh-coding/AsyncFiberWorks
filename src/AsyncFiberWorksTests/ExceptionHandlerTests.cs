using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class ExceptionHandlerTests
    {
        class StubExceptionHandler : IActionExceptionHandler
        {
            public readonly List<Exception> Failed = new List<Exception>();
            public void Handle(Exception exception)
            {
                lock (Failed)
                {
                    Failed.Add(exception);
                }
            }
        }

        class ThrowingExceptionHandler : IActionExceptionHandler
        {
            public void Handle(Exception exception)
            {
                throw new Exception("handler failure");
            }
        }

        [Test]
        public async Task ExceptionHandlerCalledWhenTaskThrows()
        {
            var handler = new StubExceptionHandler();
            var fiber = new PoolFiber(handler);
            var tcs = new TaskCompletionSource<int>();

            // This task will throw; handler should receive the exception and execution should continue.
            fiber.EnqueueTask(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("boom");
            });

            // Enqueue a subsequent task to confirm fiber continues running.
            fiber.EnqueueTask(() =>
            {
                tcs.TrySetResult(0);
                return Task.CompletedTask;
            });

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000)).ConfigureAwait(false);
            Assert.AreEqual(tcs.Task, completed, "Subsequent task did not run in time.");
            Assert.AreEqual(1, handler.Failed.Count);
            Assert.IsInstanceOf<InvalidOperationException>(handler.Failed[0]);
        }

        [Test]
        public async Task ExceptionHandlerThrowingIsSuppressed()
        {
            var handler = new ThrowingExceptionHandler();
            var fiber = new PoolFiber(handler);
            var tcs = new TaskCompletionSource<int>();

            // Cause an action to throw; handler will throw when invoked.
            fiber.EnqueueTask(async () =>
            {
                await Task.Yield();
                throw new Exception("original");
            });

            // Subsequent action should still run (handler exceptions are suppressed).
            fiber.EnqueueTask(() =>
            {
                tcs.TrySetResult(0);
                return Task.CompletedTask;
            });

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000)).ConfigureAwait(false);
            Assert.AreEqual(tcs.Task, completed, "Subsequent task did not run; handler exception may have leaked.");
        }
    }
}
