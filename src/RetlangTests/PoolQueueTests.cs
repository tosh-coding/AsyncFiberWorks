using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Retlang;

namespace RetlangTests
{
    [TestFixture]
    public class PoolQueueTests
    {
        [Test]
        public void InOrderExecution()
        {
            PoolQueue queue = new PoolQueue(new DefaultThreadPool(), new CommandExecutor());
            queue.Start();

            int count = 0;
            AutoResetEvent reset = new AutoResetEvent(false);
            List<int> result = new List<int>();
            Command command = delegate
                                  {
                                      result.Add(count++);
                                      if (count == 100)
                                      {
                                          reset.Set();
                                      }
                                  };
            for (int i = 0; i < 100; i++)
            {
                queue.Enqueue(command);
            }

            Assert.IsTrue(reset.WaitOne(10000, false));
            Assert.AreEqual(100, count);
        }
    }
}