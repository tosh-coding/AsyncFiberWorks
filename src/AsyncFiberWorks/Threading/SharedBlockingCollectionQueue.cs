using System;
using System.Collections.Concurrent;
using System.Threading;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Queue shared by multiple consumers.
    /// </summary>
    public class SharedBlockingCollectionQueue : IDedicatedConsumerThreadPoolWork
    {
        private readonly object _lock = new object();
        private readonly BlockingCollection<Action> _actions = new BlockingCollection<Action>();
        private readonly SharedBlockingCollectionQueueConsumer[] _consumerList;
        private int _disposedConsumers;

        /// <summary>
        /// Create consumers share a single queue.
        /// </summary>
        /// <param name="numberOfConsumers">Number of consumers.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SharedBlockingCollectionQueue(int numberOfConsumers = 1)
        {
            if (numberOfConsumers < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfConsumers), "Must be greater than or equal to 1");
            }

            _disposedConsumers = 0;
            _consumerList = new SharedBlockingCollectionQueueConsumer[numberOfConsumers];
            for (int i = 0; i < _consumerList.Length; i++)
            {
                _consumerList[i] = new SharedBlockingCollectionQueueConsumer(_actions, SimpleExecutor.Instance, () =>
                {
                    bool lastConsumer = false;
                    lock (_lock)
                    {
                        _disposedConsumers += 1;
                        if (_disposedConsumers >= numberOfConsumers)
                        {
                            lastConsumer = true;
                        }
                    }
                    if (lastConsumer)
                    {
                        _actions.Dispose();
                    }
                });
            }
        }

        /// <summary>
        /// Enqueue an action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _actions.Add(action);
        }

        /// <summary>
        /// Enqueue an action.
        /// </summary>
        /// <param name="action"></param>
        public void Queue(WaitCallback callback)
        {
            Enqueue(() => callback(null));
        }

        /// <summary>
        /// Created consumers.
        /// When all these consumers stop, the queue is disposed.
        /// </summary>
        public IThreadWork[] Works { get { return _consumerList; } }
    }
}
