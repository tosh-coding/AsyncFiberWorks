namespace Retlang.Channels
{
    /// <summary>
    /// Filter for messages.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageFilter<T> : IMessageFilter<T>
    {
        private Filter<T> _filterOnProducerThread;

        /// <summary>
        /// <see cref="IMessageFilter{T}.FilterOnProducerThread"/>
        /// </summary>
        public Filter<T> FilterOnProducerThread
        {
            get { return _filterOnProducerThread; }
            set { _filterOnProducerThread = value; }
        }

        /// <summary>
        /// Determine whether to pass the filter.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>True to pass, false otherwise.</returns>
        public bool PassesProducerThreadFilter(T msg)
        {
            return _filterOnProducerThread == null || _filterOnProducerThread(msg);
        }
    }
}
