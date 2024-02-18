using System;
using System.Collections.Concurrent;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Help create a queue and it's consumers.
    /// </summary>
    public class SharingQueueAndConsumerCreator
    {
        private readonly object _lock = new object();
        private readonly BlockingCollection<Action> _actions = new BlockingCollection<Action>();
        private readonly SharingQueue _queue;
        private readonly SharingQueueConsumer[] _consumerList;
        private int _disposedConsumers;

        /// <summary>
        /// Create consumers share a single queue.
        /// </summary>
        /// <param name="numberOfConsumers">Number of consumers.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SharingQueueAndConsumerCreator(int numberOfConsumers = 1)
        {
            if (numberOfConsumers < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfConsumers), "Must be greater than or equal to 1");
            }

            _disposedConsumers = 0;
            _queue = new SharingQueue(_actions);
            _consumerList = new SharingQueueConsumer[numberOfConsumers];
            for (int i = 0; i < _consumerList.Length; i++)
            {
                _consumerList[i] = new SharingQueueConsumer(_actions, new DefaultExecutor(), () =>
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
        /// The created queue.
        /// </summary>
        public IQueuingContextForThread Queue { get { return _queue; } }

        /// <summary>
        /// Created consumers.
        /// When all these consumers stop, the queue is disposed.
        /// </summary>
        public IConsumerQueueForThread[] Consumers { get { return _consumerList; } }
    }
}
