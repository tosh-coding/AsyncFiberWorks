using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// A message filter list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageFilterList<T>
    {
        private MessageFilter<T>[] _filter;
        private readonly IExecutionContext _fiber;
        private readonly Action<T> _receive;

        /// <summary>
        /// Set filters and a receiver.
        /// </summary>
        /// <param name="filters">Message pass filters.</param>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        public MessageFilterList(IEnumerable<MessageFilter<T>> filters, IExecutionContext fiber, Action<T> receive)
        {
            _filter = filters.ToArray();
            _fiber = fiber;
            _receive = receive;
        }

        /// <summary>
        /// Determine whether to pass the filter.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>True to pass, false otherwise.</returns>
        public bool PassesFilter(T msg)
        {
            foreach (var filter in _filter)
            {
                if (!filter(msg))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Message receiving function.
        /// </summary>
        /// <param name="msg"></param>
        public void Receive(T msg)
        {
            if (PassesFilter(msg))
            {
                _fiber.Enqueue(() => _receive(msg));
            }
        }
    }
}
