using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.PubSub
{
    /// <summary>
    /// A message reception handler builder.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageReceiverBuilder<T>
    {
        private readonly List<MessageFilter<T>> _filterList = new List<MessageFilter<T>>();

        /// <summary>
        /// Create a builder.
        /// </summary>
        public MessageReceiverBuilder()
        {
        }

        /// <summary>
        /// Add a message pass filter.
        /// </summary>
        /// <param name="filter">Message pass filter.</param>
        public void AddFilter(MessageFilter<T> filter)
        {
            _filterList.Add(filter);
        }

        /// <summary>
        /// Constructs a message reception handler with filters.
        /// </summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="receive">Message receiving handler.</param>
        /// <returns>The built handler.</returns>
        public Action<T> Build(IExecutionContext fiber, Action<T> receive)
        {
            var filterArray = _filterList.ToArray();
            Action<T> result = (T msg) =>
            {
                foreach (var filter in filterArray)
                {
                    if (!filter(msg))
                    {
                        return;
                    }
                }

                fiber.Enqueue(() => receive(msg));
            };
            return result;
        }
    }
}
