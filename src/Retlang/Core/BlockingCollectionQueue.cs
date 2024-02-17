using System;
using System.Collections.Concurrent;

namespace Retlang.Core
{
    /// <summary>
    /// Simple queue for actions.
    /// </summary>
    public class BlockingCollectionQueue : IQueueForThread
    {
        private readonly object _lock = new object();
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();

        private bool _running = true;

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Add(action);
        }

        private bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _running;
                }
            }
        }

        /// <summary>
        /// Execute actions until stopped.
        /// </summary>
        public void Run()
        {
            Action action;
            while (IsRunning)
            {
                action = _queue.Take();
                action();
                while (_queue.TryTake(out action))
                {
                    action();
                }
            }
        }

        /// <summary>
        /// Stop consuming the actions.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _running = false;
                Enqueue(() => { });
            }
        }
    }
}
