using NUnit.Framework;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Procedures;
using System.Threading;
using System.Threading.Tasks;
using System;
using AsyncFiberWorks.MessageFilters;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class AsyncActionDriverWithProcessedFlagEventArgsTests
    {
        [Test]
        public async Task ForwardOrder()
        {
            var channel = new AsyncActionDriver<ProcessedFlagEventArgs<int>>(new AsyncProcessedFlagExecutor<int>());

            long counter = 0;

            var fiber1 = new PoolFiber();
            Func<ProcessedFlagEventArgs<int>, Task> receiverFunc1 = async (ProcessedFlagEventArgs<int> e) =>
            {
                await fiber1.SwitchTo();
                Assert.AreEqual(123, e.Arg);
                await Task.Delay(100);
                counter = 300;
            };

            var fiber2 = new PoolFiber();
            Func<ProcessedFlagEventArgs<int>, Task> receiverFunc2 = async (ProcessedFlagEventArgs<int> e) =>
            {
                await fiber2.SwitchTo();
                Assert.AreEqual(123, e.Arg);
                counter += 1;
            };

            var disposable1 = channel.Subscribe(receiverFunc1);
            var disposable2 = channel.Subscribe(receiverFunc2);

            var eventArgs = new ProcessedFlagEventArgs<int>();
            eventArgs.Arg = 123;
            await channel.Invoke(eventArgs);
            Thread.Sleep(50);
            Assert.AreEqual(301, counter);
        }

        [Test]
        public async Task ReverseOrder()
        {
            var channel = new AsyncActionDriver<ProcessedFlagEventArgs<int>>(new AsyncProcessedFlagReverseOrderExecutor<int>());

            long counter = 0;

            var fiber1 = new PoolFiber();
            Func<ProcessedFlagEventArgs<int>, Task> receiverFunc1 = async (ProcessedFlagEventArgs<int> eventArgs) =>
            {
                await fiber1.SwitchTo();
                Assert.AreEqual(123, eventArgs.Arg);
                await Task.Delay(100);
                counter = 300;
            };

            var fiber2 = new PoolFiber();
            Func<ProcessedFlagEventArgs<int>, Task> receiverFunc2 = async (ProcessedFlagEventArgs<int> eventArgs) =>
            {
                await fiber2.SwitchTo();
                Assert.AreEqual(123, eventArgs.Arg);
                counter += 1;
            };

            var disposable1 = channel.Subscribe(receiverFunc1);
            var disposable2 = channel.Subscribe(receiverFunc2);

            var arg = new ProcessedFlagEventArgs<int>();
            arg.Arg = 123;
            await channel.Invoke(arg).ConfigureAwait(false);
            Thread.Sleep(50);
            Assert.AreEqual(300, counter);
        }

        [Test]
        public async Task DiscontinuedDuringInvoking()
        {
            var channel = new AsyncActionDriver<ProcessedFlagEventArgs<int>>(new AsyncProcessedFlagExecutor<int>());

            long counter = 0;

            Func<ProcessedFlagEventArgs<int>, Task> receiverFunc1 = async (ProcessedFlagEventArgs<int> e) =>
            {
                counter = 300;
                e.Processed = true;
                await Task.CompletedTask;
            };

            Func<ProcessedFlagEventArgs<int>, Task> receiverFunc2 = async (ProcessedFlagEventArgs<int> e) =>
            {
                counter += 1;
                e.Processed = false;
                await Task.CompletedTask;
            };

            var disposable1 = channel.Subscribe(receiverFunc1);
            var disposable2 = channel.Subscribe(receiverFunc2);

            var eventArgs = new ProcessedFlagEventArgs<int>();
            eventArgs.Arg = 123;
            await channel.Invoke(eventArgs);
            Thread.Sleep(50);
            Assert.AreEqual(300, counter);
        }
    }
}
