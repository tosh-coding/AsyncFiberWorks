using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.MessageDrivers;

namespace AsyncFiberWorks.MessageFilters
{
    /// <summary>
    /// Distribute the message to all subscribers.
    /// Call all subscriber handlers in the order in which they were registered.
    /// Wait for the calls to complete one by one before proceeding.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public class AsyncMessageDriver<TMessage> : IAsyncMessageDriver<TMessage>
    {
        private object _lock = new object();
        private LinkedList<RegisteredHandler> _actions = new LinkedList<RegisteredHandler>();
        private List<RegisteredHandler> _copiedActions = new List<RegisteredHandler>();
        bool _inInvoking = false;

        /// <summary>
        /// Create a driver.
        /// </summary>
        public AsyncMessageDriver()
        {
        }

        /// <summary>
        /// Subscribe a message driver.
        /// </summary>
        /// <param name="action">Message receiver.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Action<TMessage> action, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Action<TMessage> safeAction = (message) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    action(message);
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
        /// Subscribe a message driver.
        /// </summary>
        /// <param name="action">Message receiver.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<TMessage, Task> action, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Func<TMessage, Task> safeAction = async (message) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    await action(message);
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
        public async Task InvokeAsync(TMessage message, IFiber defaultContext)
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

            try
            {
                int nextIndex = 0;
                Func<RegisteredHandler> getNextAction = () =>
                {
                    if (nextIndex >= _copiedActions.Count)
                    {
                        return null;
                    }
                    var result = _copiedActions[nextIndex];
                    nextIndex += 1;
                    return result;
                };

                var tcs = new TaskCompletionSource<int>();
                var executionOfSimpleHandlerArray = new Action<RegisteredHandler, TMessage>[1];
                var executionOfFuncTaskHandlerArray = new Func<RegisteredHandler, TMessage, Task>[1];

                Action enqueueNextAction = () =>
                {
                    var nextAction = getNextAction();
                    if (nextAction == null)
                    {
                        defaultContext.Enqueue(() => { tcs.SetResult(0); });
                    }
                    else
                    {
                        var nextContext = nextAction.Context ?? defaultContext;
                        if (nextAction.HandlerType == HandlerType.SimpleHandler)
                        {
                            nextContext.Enqueue(() => executionOfSimpleHandlerArray[0](nextAction, message));
                        }
                        else if (nextAction.HandlerType == HandlerType.FuncTaskHandler)
                        {
                            nextContext.EnqueueTask(() => executionOfFuncTaskHandlerArray[0](nextAction, message));
                        }
                        else
                        {
                            throw new Exception($"Unknown HandlerType found. HandlerType={nextAction.HandlerType}, nextIndex={nextIndex}.");
                        }
                    }
                };

                Action<RegisteredHandler, TMessage> executionOfSimpleHandler = (currentAction, m) =>
                {
                    try
                    {
                        currentAction.SimpleHandler(m);
                    }
                    finally
                    {
                        enqueueNextAction();
                    }
                };
                executionOfSimpleHandlerArray[0] = executionOfSimpleHandler;

                Func<RegisteredHandler, TMessage, Task> executionOfFuncTaskHandler = async (currentAction, m) =>
                {
                    try
                    {
                        await currentAction.FuncTaskHandler(m).ConfigureAwait(false);
                    }
                    finally
                    {
                        enqueueNextAction();
                    }
                };
                executionOfFuncTaskHandlerArray[0] = executionOfFuncTaskHandler;

                enqueueNextAction();
                await tcs.Task.ConfigureAwait(false);
                await Task.Yield();
            }
            finally
            {
                _copiedActions.Clear();
                lock (_lock)
                {
                    _inInvoking = false;
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