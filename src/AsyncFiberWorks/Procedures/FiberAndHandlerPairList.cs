using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// List of destination fiber and handler pairs.
    /// Can specify the fiber to be executed.
    /// Call all handlers in the order in which they were registered.
    /// Wait for the calls to complete one by one before proceeding.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public class FiberAndHandlerPairList<TMessage> : ISequentialHandlerListRegistry<TMessage>, ISequentialPublisher<TMessage>
    {
        private object _lock = new object();
        private LinkedList<RegisteredHandler> _actions = new LinkedList<RegisteredHandler>();
        private List<RegisteredHandler> _copiedActions = new List<RegisteredHandler>();
        private bool _inInvoking = false;
        private int _nextIndex = 0;
        private TaskCompletionSource<int> _tcsEnd = null;
        private IFiber _defaultContext = null;
        private TMessage _message;

        /// <summary>
        /// Create a list.
        /// </summary>
        public FiberAndHandlerPairList()
        {
        }

        /// <summary>
        /// Add a handler to the tail.
        /// </summary>
        /// <param name="handler">Message handler.</param>
        /// <param name="context">The context in which the handler will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Action<TMessage> handler, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Action<TMessage> safeAction = (message) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    handler(message);
                }
            };
            var registeredHandler = new RegisteredHandler()
            {
                HandlerType = HandlerType.SimpleHandler,
                Context = context,
                SimpleHandler = safeAction,
            };

            lock (_lock)
            {
                _actions.AddLast(registeredHandler);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                maskableFilter.IsEnabled = false;
                lock (_lock)
                {
                    _actions.Remove(registeredHandler);
                }
            });

            return unsubscriber;
        }

        /// <summary>
        /// Add a handler to the tail.
        /// </summary>
        /// <param name="handler">Message handler.</param>
        /// <param name="context">The context in which the handler will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Func<TMessage, Task> handler, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Func<TMessage, Task> safeAction = async (message) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    await handler(message);
                }
            };
            var registeredAction = new RegisteredHandler()
            {
                HandlerType = HandlerType.FuncTaskHandler,
                Context = context,
                FuncTaskHandler = safeAction,
            };

            lock (_lock)
            {
                _actions.AddLast(registeredAction);
            }

            var unsubscriber = new Unsubscriber(() =>
            {
                maskableFilter.IsEnabled = false;
                lock (_lock)
                {
                    _actions.Remove(registeredAction);
                }
            });

            return unsubscriber;
        }

        /// <summary>
        /// Distribute one message.
        /// </summary>
        /// <param name="message">A message.</param>
        /// <param name="defaultContext">Default context to be used if not specified.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task PublishSequentialAsync(TMessage message, IFiber defaultContext)
        {
            lock (_lock)
            {
                if (_inInvoking)
                {
                    throw new InvalidOperationException("InvokeAsync can be called only once at the same time.");
                }

                _copiedActions.Clear();
                _copiedActions.AddRange(_actions);
                if (_copiedActions.Count <= 0)
                {
                    return;
                }
                _inInvoking = true;
            }

            _nextIndex = 0;
            _tcsEnd = new TaskCompletionSource<int>();
            _defaultContext = defaultContext;
            _message = message;

            try
            {
                enqueueNextAction();
                await _tcsEnd.Task.ConfigureAwait(false);
            }
            finally
            {
                _copiedActions.Clear();
                _tcsEnd = null;
                _defaultContext = null;
                _message = default;
                lock (_lock)
                {
                    _inInvoking = false;
                }
                await Task.Yield();
            }
        }

        RegisteredHandler getNextAction()
        {
            if (_nextIndex >= _copiedActions.Count)
            {
                return null;
            }
            var result = _copiedActions[_nextIndex];
            _nextIndex += 1;
            return result;
        }

        void enqueueNextAction()
        {
            var nextAction = getNextAction();
            if (nextAction == null)
            {
                _defaultContext.Enqueue(() => { _tcsEnd.SetResult(0); });
            }
            else
            {
                var nextContext = nextAction.Context ?? _defaultContext;
                if (nextAction.HandlerType == HandlerType.SimpleHandler)
                {
                    nextContext.Enqueue(() =>
                    {
                        try
                        {
                            nextAction.SimpleHandler(_message);
                        }
                        finally
                        {
                            enqueueNextAction();
                        }
                    });
                }
                else if (nextAction.HandlerType == HandlerType.FuncTaskHandler)
                {
                    nextContext.EnqueueTask(async () =>
                    {
                        try
                        {
                            await nextAction.FuncTaskHandler(_message).ConfigureAwait(false);
                        }
                        finally
                        {
                            enqueueNextAction();
                        }
                    });
                }
                else
                {
                    throw new Exception($"Unknown HandlerType found. HandlerType={nextAction.HandlerType}, _copiedActionsIndex={_nextIndex}.");
                }
            }
        }

        internal class RegisteredHandler
        {
            public HandlerType HandlerType;
            public IFiber Context;
            public Action<TMessage> SimpleHandler;
            public Func<TMessage, Task> FuncTaskHandler;
        }

        internal enum HandlerType
        {
            SimpleHandler,
            FuncTaskHandler,
        }
    }
}