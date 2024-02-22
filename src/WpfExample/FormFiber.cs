using System;
using System.ComponentModel;
using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace WpfExample
{
    ///<summary>
    /// Allows interaction with Windows Forms.  Transparently moves actions onto the Form's thread.
    ///</summary>
    public class FormFiber : IFiber
    {
        private readonly PoolFiber _fiber;

        /// <summary>
        /// Creates an instance.
        /// </summary>
        public FormFiber(ISynchronizeInvoke invoker, IExecutor executor)
        {
            _fiber = new PoolFiber(new FormAdapter(invoker), executor);
        }

        /// <summary>
        /// <see cref="IExecutionContext.Enqueue(Action)"/>
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _fiber.Enqueue(action);
        }

        /// <summary>
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public void Stop()
        {
            Dispose();
        }


        /// <summary>
        /// <see cref="ISubscriptionRegistry.BeginSubscription"/>
        /// </summary>
        public Unsubscriber BeginSubscription()
        {
            return _fiber.BeginSubscription();
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistry.BeginSubscriptionAndSetUnsubscriber(IDisposableSubscriptionRegistry)"/>
        /// </summary>
        public void BeginSubscriptionAndSetUnsubscriber(IDisposableSubscriptionRegistry disposable)
        {
            _fiber.BeginSubscriptionAndSetUnsubscriber(disposable);
        }

        /// <summary>
        /// <see cref="ISubscriptionRegistry.NumSubscriptions"/>
        /// </summary>
        public int NumSubscriptions
        {
            get { return _fiber.NumSubscriptions; }
        }
        /// <summary>
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public void Dispose()
        {
            _fiber?.Dispose();
        }
    }
}