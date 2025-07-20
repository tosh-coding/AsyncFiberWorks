using AsyncFiberWorks.Core;
using AsyncFiberWorks.Executors;
using AsyncFiberWorks.Fibers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// List of destination fiber and task pairs.
    /// Call all tasks in the order in which they were registered.
    /// Can specify the fiber to be executed.
    /// Wait for the calls to complete one by one before proceeding.
    /// </summary>
    public class FiberAndTaskPairList : ISequentialTaskInvoker, ISequentialTaskListRegistry
    {
        private readonly object _lock = new object();
        private readonly LinkedList<RegisteredAction> _actions = new LinkedList<RegisteredAction>();
        private readonly List<RegisteredAction> _copiedActions = new List<RegisteredAction>();
        private readonly IActionExecutor _executor;
        private bool _inInvoking = false;
        private int _nextIndex = 0;
        private TaskCompletionSource<int> _tcsEnd = null;
        private IFiber _defaultContext = null;

        /// <summary>
        /// Create a list with specified executer.
        /// </summary>
        /// <param name="executor"></param>
        public FiberAndTaskPairList(IActionExecutor executor)
        {
            _executor = executor ?? SimpleActionExecutor.Instance;
        }

        /// <summary>
        /// Create a list.
        /// </summary>
        public FiberAndTaskPairList()
            : this(null)
        {
        }

        /// <summary>
        /// Add an action to the tail.
        /// </summary>
        /// <param name="action">Action to be performed.</param>
        /// <param name="context">The context in which the action will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Action action, IFiber context = null)
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
        /// Add a task to the tail.
        /// </summary>
        /// <param name="task">Task to be performed.</param>
        /// <param name="context">The context in which the task will execute. if null, the default is used.</param>
        /// <returns>Handle for canceling registration.</returns>
        public IDisposable Add(Func<Task> task, IFiber context = null)
        {
            var maskableFilter = new ToggleFilter();
            Func<Task> safeAction = async () =>
            {
                var enabled = maskableFilter.IsEnabled;
                if (enabled)
                {
                    await task();
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
        /// Invoke all tasks sequentially.
        /// </summary>
        /// <param name="defaultContext">Default context to be used if not specified.</param>
        /// <returns>A task that waits for tasks to be performed.</returns>
        public async Task InvokeSequentialAsync(IFiber defaultContext)
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
                lock (_lock)
                {
                    _inInvoking = false;
                }
                await Task.Yield();
            }
        }

        RegisteredAction getNextAction()
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
                if (nextAction.ActionType == ActionType.SimpleAction)
                {
                    nextContext.Enqueue(() =>
                    {
                        try
                        {
                            _executor.Execute(nextAction.SimpleAction);
                        }
                        finally
                        {
                            enqueueNextAction();
                        }
                    });
                }
                else if (nextAction.ActionType == ActionType.FuncTask)
                {
                    nextContext.EnqueueTask(async () =>
                    {
                        try
                        {
                            await _executor.Execute(nextAction.FuncTask).ConfigureAwait(false);
                        }
                        finally
                        {
                            enqueueNextAction();
                        }
                    });
                }
                else
                {
                    throw new Exception($"Unknown ActionType found. ActionType={nextAction.ActionType}, nextIndex={_nextIndex}.");
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
