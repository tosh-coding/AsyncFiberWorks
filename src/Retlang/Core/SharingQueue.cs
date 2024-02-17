using System;
using System.Collections.Concurrent;

namespace Retlang.Core
{
    /// <summary>
    /// Queue shared by multiple consumers.
    /// </summary>
    internal class SharingQueue : IQueuingContextForThread
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
        /// <see cref="IQueuingContextForThread.Enqueue(Action)"/>
        /// </summary>
        public void Enqueue(Action action)
        {
            _actions.Add(action);
        }
    }
}
