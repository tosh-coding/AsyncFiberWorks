using AsyncFiberWorks.Core;
using AsyncFiberWorks.MessageFilters;
using AsyncFiberWorks.Procedures;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class ActionDriverTests
    {
        [Test]
        public void Invoking()
        {
            var driver = new ActionDriver();

            long counter = 0;

            Action action1 = () =>
            {
                Assert.AreEqual(0, counter);
                counter = 300;
            };

            Action action2 = () =>
            {
                Assert.AreEqual(300, counter);
                counter += 1;
            };

            var disposable1 = driver.Subscribe(action1);
            var disposable2 = driver.Subscribe(action2);

            driver.Invoke();
            Assert.AreEqual(301, counter);
        }

        [Test]
        public async Task AsyncInvoking()
        {
            var driver = new AsyncActionDriver();

            long counter = 0;

            Func<Task> action1 = async () =>
            {
                Assert.AreEqual(0, counter);
                await Task.Delay(100).ConfigureAwait(false);
                counter = 300;
            };

            Func<Task> action2 = async () =>
            {
                await Task.Yield();
                Assert.AreEqual(300, counter);
                counter += 1;
            };

            var disposable1 = driver.Subscribe(action1);
            var disposable2 = driver.Subscribe(action2);

            await driver.Invoke();
            Assert.AreEqual(301, counter);
        }

        [Test]
        public async Task AsyncInvokingWithArgument()
        {
            var driver = new AsyncMessageDriver<int>();

            long counter = 0;

            Func<int, Task> action1 = async (value) =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                counter += value;
            };

            Func<int, Task> action2 = async (value) =>
            {
                await Task.Yield();
                counter += value / 10;
            };

            var disposable1 = driver.Subscribe(action1);
            var disposable2 = driver.Subscribe(action2);

            await driver.Invoke(200);
            Assert.AreEqual(200 + 20, counter);
            await driver.Invoke(10);
            Assert.AreEqual(200 + 20 + 10 + 1, counter);
        }

        [Test]
        public void ToggleAtUnsubscribe()
        {
            var driver = new ActionDriver();

            long counter = 0;

            var unsubscriber = new Unsubscriber();

            Action action = () =>
            {
                counter += 1;
                unsubscriber.Dispose();
            };

            var disposable1 = driver.Subscribe(action);
            unsubscriber.AppendDisposable(disposable1);
            var disposable2 = driver.Subscribe(action);
            unsubscriber.AppendDisposable(disposable2);

            driver.Invoke();
            Assert.AreEqual(1, counter);
        }
    }
}
