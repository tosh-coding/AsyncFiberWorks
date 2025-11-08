using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// List of destination fiber and handler pairs.
    /// Can specify the fiber to be executed.
    /// Call all handlers in the order in which they were registered.
    /// Wait for the calls to complete one by one before proceeding.
    /// If it has not yet been processed, it proceeds to the next step. If already processed, exit at that point.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public class FiberAndHandlerPairList<TMessage> : ISequentialPublisher<TMessage>
    {
        private readonly object _lock = new object();
        private readonly LinkedList<RegisteredHandler> _actions = new LinkedList<RegisteredHandler>();
        private readonly List<RegisteredHandler> _copiedActions = new List<RegisteredHandler>();
        private readonly IActionExecutor _executor;
        private readonly ProcessedFlagEventArgs<TMessage> _processedEventArg = new ProcessedFlagEventArgs<TMessage>();
        private bool _inInvoking = false;
        private int _nextIndex = 0;
        private TaskCompletionSource<int> _tcsEnd = null;
        private IFiber _defaultContext = null;

        /// <summary>
        /// Create a list with specified executer.
        /// </summary>
        /// <param name="executor"></param>
        public FiberAndHandlerPairList(IActionExecutor executor)
        {
            _executor = executor ?? SimpleExecutor.Instance;
        }

        /// <summary>
        /// Create a list.
        /// </summary>
        public FiberAndHandlerPairList()
            : this(null)
        {
        }

        /// <summary>
        /// Add a handler to the tail.
        /// </summary>
        /// <param name="handler">Message handler. The return value is the processed flag. If this flag is true, subsequent handlers are not called.</param>
        /// <param name="context">The context in which the handler will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Func<TMessage, bool> handler, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Func<TMessage, bool> safeAction = (message) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    return handler(message);
                }
                return false;
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
        /// <param name="handler">Message handler. The return value is the processed flag. If this flag is true, subsequent handlers are not called.</param>
        /// <param name="context">The context in which the handler will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Action<IFiberExecutionEventArgs, ProcessedFlagEventArgs<TMessage>> handler, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Action<IFiberExecutionEventArgs, ProcessedFlagEventArgs<TMessage>> safeAction = (e, processedEventArgs) =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    handler(e, processedEventArgs);
                }
            };
            var registeredAction = new RegisteredHandler()
            {
                HandlerType = HandlerType.EventArgHandler,
                Context = context,
                EventArgHandler = safeAction,
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
        /// Add a handler to the tail.
        /// </summary>
        /// <param name="handler">Message handler. The return value is the processed flag. If this flag is true, subsequent handlers are not called.</param>
        /// <param name="context">The context in which the handler will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Func<TMessage, Task<bool>> handler, IFiber context = null)
        {
            return this.Add((IFiberExecutionEventArgs e, ProcessedFlagEventArgs<TMessage> message) => e.PauseWhileRunning(async () =>
            {
                message.Processed = await handler(message.Arg).ConfigureAwait(false);
            }), context);
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
            _tcsEnd = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _defaultContext = defaultContext;
            _processedEventArg.Arg = message;
            _processedEventArg.Processed = false;

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
                _processedEventArg.Arg = default;
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
                enqueueEndAction();
            }
            else
            {
                var nextContext = nextAction.Context ?? _defaultContext;
                if (nextAction.HandlerType == HandlerType.SimpleHandler)
                {
                    nextContext.Enqueue(() =>
                    {
                        bool isProcessed = false;
                        try
                        {
                            _executor.Execute(() =>
                            {
                                isProcessed = nextAction.SimpleHandler(_processedEventArg.Arg);
                            });
                        }
                        finally
                        {
                            if (!isProcessed)
                            {
                                enqueueNextAction();
                            }
                            else
                            {
                                enqueueEndAction();
                            }
                        }
                    });
                }
                else if (nextAction.HandlerType == HandlerType.EventArgHandler)
                {
                    nextContext.Enqueue((e) =>
                    {
                        _executor.Execute(e, (arg) =>
                        {
                            var eventArgs = new FiberAndTaskPairList.EnqueueNextActionEventArgs(e, () =>
                            {
                                if (!_processedEventArg.Processed)
                                {
                                    enqueueNextAction();
                                }
                                else
                                {
                                    enqueueEndAction();
                                }
                            });
                            try
                            {
                                nextAction.EventArgHandler(eventArgs, _processedEventArg);
                            }
                            finally
                            {
                                eventArgs.CheckAndEnqueue();
                            }
                        });
                    });
                }
                else
                {
                    throw new Exception($"Unknown HandlerType found. HandlerType={nextAction.HandlerType}, _copiedActionsIndex={_nextIndex}.");
                }
            }

            void enqueueEndAction()
            {
                _tcsEnd.SetResult(0);
            }
        }

        internal class RegisteredHandler
        {
            public HandlerType HandlerType;
            public IFiber Context;
            public Func<TMessage, bool> SimpleHandler;
            public Action<IFiberExecutionEventArgs, ProcessedFlagEventArgs<TMessage>> EventArgHandler;
        }

        internal enum HandlerType
        {
            SimpleHandler,
            EventArgHandler
        }
    }
}