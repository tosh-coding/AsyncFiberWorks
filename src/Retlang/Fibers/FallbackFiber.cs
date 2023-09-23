using System;
using System.Collections.Generic;
using Retlang.Core;

namespace Retlang.Fibers
{
    /// <summary>
    /// Switch to the default PoolFiber when the fiber ends.
    /// </summary>
    public class FallbackFiber : IFiber
    {
        private readonly object _lock = new object();
        private readonly bool _isDisposable;

        private IExecutionContext _fiber;
        private IDisposable _disposable;
        private Queue<Action> _switchingQueue = null;

        /// <summary>
        /// Added the ability to fall back when the fiber ends.
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="disposable"></param>
        public FallbackFiber(IExecutionContext fiber, IDisposable disposable)
        {
            _isDisposable = disposable != null;
            _fiber = fiber;
            _disposable = disposable;
        }

        /// <summary>
        /// Wrapping never-ending fibers.
        /// </summary>
        /// <param name="fiber"></param>
        public FallbackFiber(IExecutionContext fiber)
        {
            _isDisposable = false;
            _fiber = fiber;
        }

        /// <summary>
        /// Always null. This is not used because the fiber falls back.
        /// </summary>
        public ISubscriptionRegistry FallbackDisposer { get { return null; } }

        /// <summary>
        /// <see cref="IExecutionContext.Enqueue(Action)"/>
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            if (!_isDisposable)
            {
                _fiber.Enqueue(action);
            }
            else
            {
                lock (_lock)
                {
                    if (_switchingQueue != null)
                    {
                        _switchingQueue.Enqueue(action);
                    }
                    else
                    {
                        _fiber.Enqueue(action);
                    }
                }
            }
        }

        /// <summary>
        /// Switch to the fallback.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposable)
            {
                return;
            }

            lock (_lock)
            {
                if (_disposable == null)
                {
                    return;
                }

                var tmpDisposable = _disposable;
                _disposable = null;
                _switchingQueue = new Queue<Action>();

                _fiber.Enqueue(() =>
                {
                    tmpDisposable.Dispose();
                    var poolFiber = new PoolFiberSlim();

                    lock (_lock)
                    {
                        while (_switchingQueue.Count > 0)
                        {
                            poolFiber.Enqueue(_switchingQueue.Dequeue());
                        }
                        _fiber = poolFiber;
                        _switchingQueue = null;
                    }
                });
            }
        }
    }
}
