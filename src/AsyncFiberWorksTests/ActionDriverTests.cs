using AsyncFiberWorks.Procedures;
using NUnit.Framework;
using System;

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
    }
}
