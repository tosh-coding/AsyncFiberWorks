﻿using NUnit.Framework;
using Retlang.Fibers;
using System.Threading;

namespace RetlangTests
{
    [TestFixture]
    public class FiberPauseResumeTests
    {
        [Test]
        public void PauseAndResumePoolFiber()
        {
            var fiber = new PoolFiberSlim();
            int counter = 0;
            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            fiber.Pause();
            fiber.Enqueue(() => counter += 1);
            Thread.Sleep(1);
            Assert.AreEqual(1, counter);

            fiber.Resume(() => counter = 5);
            Thread.Sleep(1);
            Assert.AreEqual(6, counter);
        }
    }
}
