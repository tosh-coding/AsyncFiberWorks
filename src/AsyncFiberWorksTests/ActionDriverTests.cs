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
    }
}
