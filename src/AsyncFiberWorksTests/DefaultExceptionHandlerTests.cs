using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class DefaultExceptionHandlerTests
    {
        [Test]
        public async Task NullHandler_DoesNotBreakFiberExecution()
        {
            var fiber = new PoolFiber((IActionExceptionHandler)null);
            var tcs = new TaskCompletionSource<int>();

            fiber.EnqueueTask(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("boom-null");
            });

            fiber.EnqueueTask(() =>
            {
                tcs.TrySetResult(0);
                return Task.CompletedTask;
            });

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000)).ConfigureAwait(false);
            Assert.AreEqual(tcs.Task, completed, "Subsequent task did not run; null handler may have caused an error.");
        }

        [Test]
        public void DefaultHandler_LogsToTrace()
        {
            var sw = new StringWriter();
            var listener = new TextWriterTraceListener(sw);
            Trace.Listeners.Add(listener);
            try
            {
                var ex = new Exception("log-test");
                DefaultActionExceptionHandler.Instance.Handle(ex);
                Trace.Flush();
                var output = sw.ToString();
                Assert.IsTrue(output.Contains("AsyncFiberWorks"), "Trace output does not contain marker.");
                Assert.IsTrue(output.Contains("log-test"), "Trace output does not contain exception message.");
            }
            finally
            {
                Trace.Listeners.Remove(listener);
                listener.Dispose();
            }
        }
    }
}
