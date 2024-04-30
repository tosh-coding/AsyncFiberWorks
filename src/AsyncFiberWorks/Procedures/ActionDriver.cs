using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default driver implementation. Invokes all of the subscriber's actions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionDriver : IActionDriver
    {
        private readonly ActionList _actions = new ActionList();

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Action action)
        {
            return _actions.AddHandler(action);
        }

        /// <summary>
        /// <see cref="IActionInvoker.Invoke"/>
        /// </summary>
        public void Invoke()
        {
            _actions.Invoke();
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _actions.Count; } }
    }
}
