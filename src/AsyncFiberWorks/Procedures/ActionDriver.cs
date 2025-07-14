using AsyncFiberWorks.Core;
using AsyncFiberWorks.Executors;
using AsyncFiberWorks.Fibers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Invokes all of the subscriber's actions.
    /// </summary>
    public class ActionDriver : IActionDriver
    {
        private readonly object _lock = new object();
        private readonly LinkedList<RegisteredAction> _actions = new LinkedList<RegisteredAction>();
        private readonly List<RegisteredAction> _copiedActions = new List<RegisteredAction>();
        IActionExecutor _executor;
        bool _inInvoking = false;

        /// <summary>
        /// Create an action driver.
        /// </summary>
        /// <param name="executor"></param>
        public ActionDriver(IActionExecutor executor)
        {
            _executor = executor ?? SimpleActionExecutor.Instance;
        }

        /// <summary>
        /// Create an action driver.
        /// </summary>
        public ActionDriver()
            : this(null)
        {
        }

        /// <summary>
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Action action, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Action safeAction = () =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    action();
                }
            };
            var registeredAction = new RegisteredAction()
            {
                ActionType = ActionType.SimpleAction,
                Context = context,
                SimpleAction = safeAction,
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
        /// Subscribe an action driver.
        /// </summary>
        /// <param name="action">Subscriber.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Unsubscriber.</returns>
        public IDisposable Subscribe(Func<Task> action, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Func<Task> safeAction = async () =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    await action();
                }
            };
            var registeredAction = new RegisteredAction()
            {
                ActionType = ActionType.FuncTask,
                Context = context,
                FuncTask = safeAction,
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
        /// Invoke all subscribers.
        /// </summary>
        /// <param name="defaultContext">Default context to be used if not specified.</param>
        /// <returns>A task that waits for actions to be performed.</returns>
        public async Task InvokeAsync(IFiber defaultContext)
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
                Func<RegisteredAction> getNextAction = () =>
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
                var executionOfSimpleActionArray = new Action<RegisteredAction>[1];
                var executionOfFuncTaskArray = new Func<RegisteredAction, Task>[1];

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
                        if (nextAction.ActionType == ActionType.SimpleAction)
                        {
                            nextContext.Enqueue(() => executionOfSimpleActionArray[0](nextAction));
                        }
                        else if (nextAction.ActionType == ActionType.FuncTask)
                        {
                            nextContext.EnqueueTask(() => executionOfFuncTaskArray[0](nextAction));
                        }
                        else
                        {
                            throw new Exception($"Unknown ActionType found. ActionType={nextAction.ActionType}, nextIndex={nextIndex}.");
                        }
                    }
                };

                Action<RegisteredAction> executionOfSimpleAction = (currentAction) =>
                {
                    try
                    {
                        _executor.Execute(currentAction.SimpleAction);
                    }
                    finally
                    {
                        enqueueNextAction();
                    }
                };
                executionOfSimpleActionArray[0] = executionOfSimpleAction;

                Func<RegisteredAction, Task> executionOfFuncTask = async (currentAction) =>
                {
                    try
                    {
                        await _executor.Execute(currentAction.FuncTask).ConfigureAwait(false);
                    }
                    finally
                    {
                        enqueueNextAction();
                    }
                };
                executionOfFuncTaskArray[0] = executionOfFuncTask;

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

        internal class RegisteredAction
        {
            public ActionType ActionType;
            public IFiber Context;
            public Action SimpleAction;
            public Func<Task> FuncTask;
        }

        internal enum ActionType
        {
            SimpleAction,
            FuncTask,
        }
    }
}
