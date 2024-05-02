using System;
using System.Collections.Concurrent;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Queue shared by multiple consumers.
    /// </summary>
    public class SharingQueue
    {
        private readonly BlockingCollection<Action> _actions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actions"></param>
        public SharingQueue(BlockingCollection<Action> actions)
        {
            _actions = actions;
        }

        /// <summary>
        /// Enqueue an action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _actions.Add(action);
        }
    }
}
