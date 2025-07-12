using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Invokes all of the subscriber's actions.
    /// </summary>
    public class ActionDriver : IActionDriver
    {
        private readonly object _lock = new object();
        private readonly LinkedList<Action<FiberExecutionEventArgs>> _actions = new LinkedList<Action<FiberExecutionEventArgs>>();
        private readonly AsyncTaskQueueLoop _fiber = new AsyncTaskQueueLoop();

        /// <summary>
        /// Create an action driver.
        /// </summary>
        public ActionDriver()
        {
        }

        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Action action)
        {
            return Subscribe((e) => action());
        }

        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Action<FiberExecutionEventArgs> action)
        {
            var maskableFilter = new ToggleFilter();
            Action<FiberExecutionEventArgs> safeAction = (e) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    action(e);
                }
            };

            lock (_lock)
            {
                _actions.AddLast(safeAction);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                maskableFilter.IsEnabled = false;
                _actions.Remove(safeAction);
            });

            return unsubscriber;
        }

        /// <summary>
        /// Invoke all subscribers.
        /// Fibers passed as arguments will be paused.
        /// </summary>
        /// <param name="eventArgs">Handle for fiber pause.</param>
        public void InvokeAsync(FiberExecutionEventArgs eventArgs)
        {
            eventArgs.Pause();
            lock (_lock)
            {
                foreach (var action in _actions)
                {
                    _fiber.Enqueue(eventArgs.SourceThread, action);
                }
            }
            _fiber.Enqueue(() => eventArgs.Resume());
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers
        {
            get
            {
                lock (_lock)
                {
                    return _actions.Count;
                }
            }
        }
    }
}
