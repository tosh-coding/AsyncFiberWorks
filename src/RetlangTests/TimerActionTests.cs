using System;
using System.Threading;
using NUnit.Framework;
using Retlang.Core;
using Rhino.Mocks;

namespace RetlangTests
{
    [TestFixture]
    public class TimerActionTests
    {
        [Test]
        public void Cancel()
        {
            var executionCount = 0;
            Action action = () => executionCount++;
            var timer = new TimerAction(action, 1, 2);
            timer.ExecuteOnFiberThread();
            Assert.AreEqual(1, executionCount);
            timer.Dispose();
            timer.ExecuteOnFiberThread();

            Assert.AreEqual(1, executionCount);
        }

        [Test]
        public void CallbackFromTimer()
        {
            var mocks = new MockRepository();

            var action = mocks.StrictMock<Action>();
            var timer = new TimerAction(action, 2, 3);
            var registry = mocks.StrictMock<ISchedulerRegistry>();
            registry.Enqueue(timer.ExecuteOnFiberThread);

            mocks.ReplayAll();

            timer.ExecuteOnTimerThread(registry);
        }

        [Test]
        public void CallbackFromIntervalTimerWithCancel()
        {
            var mocks = new MockRepository();
            var action = mocks.StrictMock<Action>();
            var timer = new TimerAction(action, 2, 3);
            var registry = mocks.StrictMock<ISchedulerRegistry>();

            registry.Remove(timer);

            mocks.ReplayAll();

            timer.Dispose();
            timer.ExecuteOnTimerThread(registry);
        }

        [Test]
        public void CallbackFromTimerWithCancel()
        {
            var mocks = new MockRepository();
            var action = mocks.StrictMock<Action>();
            var timer = new TimerAction(action, 2, Timeout.Infinite);
            var registry = mocks.StrictMock<ISchedulerRegistry>();

            registry.Remove(timer);
            registry.Enqueue(timer.ExecuteOnFiberThread);

            mocks.ReplayAll();
            timer.ExecuteOnTimerThread(registry);
        }
    }
}