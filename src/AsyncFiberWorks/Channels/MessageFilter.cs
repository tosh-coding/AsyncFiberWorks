using System.Collections.Generic;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// A message filter that run in the producer/publisher thread.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageFilter<T> : IMessageFilter<T>
    {
        private List<Filter<T>> _filterOnProducerThread;

        /// <summary>
        /// Add a filter.
        /// </summary>
        /// <param name="filter"></param>
        public void AddFilterOnProducerThread(Filter<T> filter)
        {
            if (_filterOnProducerThread == null)
            {
                _filterOnProducerThread = new List<Filter<T>>();
            }
            _filterOnProducerThread.Add(filter);
        }

        /// <summary>
        /// Determine whether to pass the filter.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>True to pass, false otherwise.</returns>
        public bool PassesProducerThreadFilter(T msg)
        {
            if (_filterOnProducerThread == null)
            {
                return true;
            }
            foreach (var filter in _filterOnProducerThread)
            {
                if (!filter(msg))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
