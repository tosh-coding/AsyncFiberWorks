using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests.Examples
{
    [TestFixture]
    public class GameObjectExamples
    {
        [Test]
        public void PlainUpdateLoopTest()
        {
            var gameObjects = new List<GameObjectCounter>
            {
                new GameObjectCounter { Name = "Player" },
                new GameObjectCounter { Name = "Enemy" },
                new GameObjectCounter { Name = "NPC" },
            };
            for (int i = 0; i < 10; i++)
            {
                foreach (var gameObject in gameObjects)
                {
                    gameObject.Update();
                }
            }

            foreach (var gameObject in gameObjects)
            {
                Assert.AreEqual(10, gameObject.Counter);
            }
        }

        public class GameObjectCounter
        {
            public string Name { get; set; }
            public int Counter { get; private set; }

            public void Update()
            {
                Counter++;
            }
        }

        [Test]
        public async Task OneShotTaskTest()
        {
            var gameObject = new GameObjectWithTask { Name = "Loader" };
            for (int i = 0; i < 10; i++)
            {
                gameObject.Update();
                await Task.Delay(10).ConfigureAwait(false);
            }

            Assert.IsTrue(gameObject.LongTask.IsCompleted);
            Assert.AreNotEqual(gameObject.ThreadId1, gameObject.ThreadId2);
            Assert.AreNotEqual(gameObject.ThreadId2, gameObject.ThreadId3);
        }

        public class GameObjectWithTask
        {
            public string Name { get; set; }
            readonly ConcurrentQueueActionQueue ActionQueue = new ConcurrentQueueActionQueue();
            public Task LongTask;
            PoolFiber Fiber;
            public int ThreadId1 { get; private set; } = -1;
            public int ThreadId2 { get; private set; } = -1;
            public int ThreadId3 { get; private set; } = -1;

            public GameObjectWithTask()
            {
                this.Fiber = new PoolFiber(new ThreadPoolAdapter(ActionQueue));
                this.LongTask = AnTask();
            }

            public void Update()
            {
                this.ActionQueue.ExecuteNextBatch();
            }

            async Task AnTask()
            {
                await Task.Delay(10).ConfigureAwait(false);
                this.ThreadId1 = Thread.CurrentThread.ManagedThreadId;
                await this.Fiber.EnqueueAsync(() =>
                {
                    this.ThreadId2 = Thread.CurrentThread.ManagedThreadId;
                }).ConfigureAwait(false);
                await Task.Delay(10).ConfigureAwait(false);
                this.ThreadId3 = Thread.CurrentThread.ManagedThreadId;
            }
        }

        [Test]
        public void AsyncMainLoopTest()
        {
            var gameObject = new GameObjectMainLoop { Name = "DontDestroyObject" };
            var counterSnapshot = new List<long>();
            for (int i = 0; i < 3; i++)
            {
                gameObject.Update();
                counterSnapshot.Add(gameObject.Counter);
            }

            Assert.AreEqual(3, counterSnapshot.Count);
            Assert.AreEqual(1, counterSnapshot[0]);
            Assert.AreEqual(2, counterSnapshot[1]);
            Assert.AreEqual(3, counterSnapshot[2]);
        }

        public class GameObjectMainLoop
        {
            public string Name { get; set; }
            readonly BlockingConsumerYieldable ActionQueue = new BlockingConsumerYieldable();
            public Task LongTask;
            public long Counter = 0;

            public GameObjectMainLoop()
            {
                this.LongTask = MainLoopAsync();
            }

            public void Update()
            {
                this.ActionQueue.RunUntilYield();
            }

            async Task MainLoopAsync()
            {
                Counter = 0;
                await this.ActionQueue.Yield().ConfigureAwait(false);
                do
                {
                    this.Counter += 1;
                    await this.ActionQueue.Yield().ConfigureAwait(false);
                } while (this.Counter < 10);
                this.Counter += 1;
            }
        }

        [Test]
        public void CoroutineTest()
        {
            int numYields = 5;
            var gameObjectList = new List<GameObjectCoroutine>
            {
                new GameObjectCoroutine(name: "Player", numYields),
                new GameObjectCoroutine(name: "Enemy", numYields),
                new GameObjectCoroutine(name: "NPC", numYields),
            };
            var counterSnapshotList = new List<List<long>>(gameObjectList.Count);
            for (int i = 0; i < gameObjectList.Count; i++)
            {
                counterSnapshotList.Add(new List<long>());
            }

            while (true)
            {
                for (int j = 0; j < gameObjectList.Count; j++)
                {
                    gameObjectList[j].Update();
                    counterSnapshotList[j].Add(gameObjectList[j].Counter);
                }
                if (gameObjectList.Sum(x => x.IsEnd ? 1 : 0) == gameObjectList.Count)
                {
                    break;
                }
            }

            for (int j = 0; j < gameObjectList.Count; j++)
            {
                var snapshots = counterSnapshotList[j];
                Assert.AreEqual(numYields, snapshots.Count);
                for (int y = 0; y < numYields; y++)
                {
                    Assert.AreEqual(y + 1, snapshots[y]);
                }
            }
        }

        public class GameObjectCoroutine
        {
            public string Name { get; private set; }
            readonly BlockingConsumerYieldable ActionQueue;
            public Task LongTask;
            public long Counter;
            public bool IsEnd;
            public int NumYields { get; private set; }

            public GameObjectCoroutine(string name, int numYields)
            {
                this.Name = name;
                this.NumYields = numYields;
                this.ActionQueue = new BlockingConsumerYieldable();
                this.Counter = 0;
                this.LongTask = MainLoopAsync(this.ActionQueue);
            }

            public void Update()
            {
                this.ActionQueue.RunUntilYield();
                this.IsEnd = this.LongTask.IsCompleted;
            }

            async Task MainLoopAsync(BlockingConsumerYieldable queue)
            {
                this.Counter = 0;
                await queue.Yield().ConfigureAwait(false);
                do
                {
                    this.Counter += 1;
                    await queue.Yield().ConfigureAwait(false);
                } while (Counter < (this.NumYields - 1));
                this.Counter += 1;
            }
        }

        [Test]
        public void MultipleCoroutineTest()
        {
            int numCoroutines = 2;
            int numYields = 5;
            var gameObjectList = new List<GameObjectMultipleCoroutine>
            {
                new GameObjectMultipleCoroutine(name: "Player", numCoroutines, numYields),
                new GameObjectMultipleCoroutine(name: "Enemy", numCoroutines, numYields),
                new GameObjectMultipleCoroutine(name: "NPC", numCoroutines, numYields),
            };
            var counterSnapshotList = new List<List<long[]>>(gameObjectList.Count);
            for (int i = 0; i < gameObjectList.Count; i++)
            {
                counterSnapshotList.Add(new List<long[]>());
            }
            while (true)
            {
                for (int j = 0; j < gameObjectList.Count; j++)
                {
                    gameObjectList[j].Update();
                    counterSnapshotList[j].Add(gameObjectList[j].Counter.Select(x => (long)x).ToArray());
                }
                if (gameObjectList.Sum(x => x.IsEnd ? 1 : 0) == gameObjectList.Count)
                {
                    break;
                }
            }

            for (int j = 0; j < gameObjectList.Count; j++)
            {
                var counterSnapshot = counterSnapshotList[j];
                Assert.AreEqual(numYields, counterSnapshot.Count);
                for (int y = 0; y < numYields; y++)
                {
                    for (var c = 0; c < numCoroutines; c++)
                    {
                        Assert.AreEqual(y + 1, counterSnapshot[y][c]);
                    }
                }
            }
        }

        public class GameObjectMultipleCoroutine
        {
            public string Name { get; private set; }
            readonly List<BlockingConsumerYieldable> ActionQueueList;
            public List<Task> LongTaskList;
            public List<long> Counter = new List<long>();
            public bool IsEnd;
            public int NumCoroutines { get; private set; }
            public int NumYields { get; private set; }

            public GameObjectMultipleCoroutine(string name, int numCoroutines, int numYields)
            {
                this.Name = name;
                this.NumCoroutines = numCoroutines;
                this.NumYields = numYields;
                this.ActionQueueList = new List<BlockingConsumerYieldable>(numCoroutines);
                this.Counter = new List<long>(numCoroutines);
                this.LongTaskList = new List<Task>(numCoroutines);
                for (int i = 0; i < numCoroutines; i++)
                {
                    this.ActionQueueList.Add(new BlockingConsumerYieldable());
                    this.Counter.Add(0);
                    var t = MainLoopAsync(this.ActionQueueList[i], i);
                    this.LongTaskList.Add(t);
                }
            }

            public void Update()
            {
                for (int i = 0; i < this.ActionQueueList.Count; i++)
                {
                    this.ActionQueueList[i].RunUntilYield();
                }
                this.IsEnd = this.LongTaskList.Count(x => x.IsCompleted) == this.LongTaskList.Count;
            }

            async Task MainLoopAsync(BlockingConsumerYieldable queue, int index)
            {
                this.Counter[index] = 0;
                await queue.Yield().ConfigureAwait(false);
                do
                {
                    this.Counter[index] += 1;
                    await queue.Yield().ConfigureAwait(false);
                } while (this.Counter[index] < (this.NumYields - 1));
                this.Counter[index] += 1;
            }
        }
    }
}
