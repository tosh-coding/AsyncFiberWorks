using System;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// The same instance of this class will not be executed concurrently.
    /// The one executed later is skipped.
    /// </summary>
    public class NonReentrantFiberFilter : IExecutionContext
    {
        private readonly object _lockObj = new object();
        private readonly IExecutionContext _fiber;
        private bool _executing = false;

        /// <summary>
        /// Create a filter.
        /// </summary>
        /// <param name="fiber"></param>
        public NonReentrantFiberFilter(IExecutionContext fiber)
        {
            _fiber = fiber;
        }

        /// <summary>
        /// Enqueue an action.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        public void Enqueue(Action action)
        {
            lock (_lockObj)
            {
                if (_executing)
                {
                    return;
                }
                _executing = true;
            }

            _fiber.Enqueue(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                    lock (_lockObj)
                    {
                        _executing = false;
                    }
                }
            });
        }
    }
}
