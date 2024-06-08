using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Default driver implementation. Invokes all of the subscriber's actions.
    /// </summary>
    public class ActionDriver : IActionDriver
    {
        private readonly ActionList _actions = new ActionList();
        private readonly IExecutor _executor;

        /// <summary>
        /// Create a driver.
        /// </summary>
        /// <param name="executor"></param>
        public ActionDriver(IExecutor executor = default)
        {
            _executor = executor;
        }

        /// <summary>
        /// Subscribe a channel.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Action action)
        {
            if (_executor != null)
            {
                return _actions.AddHandler(() =>
                {
                    _executor.Execute(action);
                });
            }
            else
            {
                return _actions.AddHandler(action);
            }
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
