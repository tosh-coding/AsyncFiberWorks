using AsyncFiberWorks.Channels;
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
                var maskableFilter = new MaskableExecutor();
                var disposable = _actions.AddHandler(() => maskableFilter.Execute(() => _executor.Execute(action)));
                return new Unsubscriber(() =>
                {
                    maskableFilter.IsEnabled = false;
                    disposable.Dispose();
                });
            }
            else
            {
                var maskableFilter = new MaskableExecutor();
                var disposable = _actions.AddHandler(() => maskableFilter.Execute(action));
                return new Unsubscriber(() =>
                {
                    maskableFilter.IsEnabled = false;
                    disposable.Dispose();
                });
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
