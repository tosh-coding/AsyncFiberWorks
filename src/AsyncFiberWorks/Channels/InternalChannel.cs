using System;

namespace AsyncFiberWorks.Channels
{
    ///<summary>
    /// A channel for internal. Methods are thread safe.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    internal sealed class InternalChannel<T>
    {
        private event Action<T> _subscribers;

        /// <summary>
        /// Subscribes an action to be executed for every action posted to the channel. Action should be thread safe. 
        /// Action may be invoked on multiple threads.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IDisposable SubscribeOnProducerThreads(Action<T> action)
        {
            _subscribers += action;

            var unsubscriber = new Unsubscriber(() => {
                this._subscribers -= action;
            });

            return unsubscriber;
        }

        /// <summary>
        /// Publish a message to all subscribers. Returns true if any subscribers are registered.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Publish(T msg)
        {
            var evnt = _subscribers; // copy reference for thread safety
            if (evnt != null)
            {
                evnt(msg);
                return true;
            }
            return false;
        }

        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers
        {
            get
            {
                var evnt = _subscribers; // copy reference for thread safety
                return evnt == null ? 0 : evnt.GetInvocationList().Length;
            }
        }
    }
}
