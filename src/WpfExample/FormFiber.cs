using System;
using System.ComponentModel;
using Retlang.Core;
using Retlang.Fibers;

namespace WpfExample
{
    ///<summary>
    /// Allows interaction with Windows Forms.  Transparently moves actions onto the Form's thread.
    ///</summary>
    public class FormFiber : IFiber
    {
        private readonly IFiberSlim _fiber;
        private readonly Subscriptions _subscriptions = new Subscriptions();

        /// <summary>
        /// Creates an instance.
        /// </summary>
        public FormFiber(ISynchronizeInvoke invoker, IExecutor executor)
        {
            _fiber = new PoolFiberSlim(new FormAdapter(invoker), executor);
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
        /// <see cref="ISubscriptionRegistryGetter.FallbackDisposer"/>
        /// </summary>
        public ISubscriptionRegistry FallbackDisposer
        {
            get { return _subscriptions; }
        }

        /// <summary>
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
        }
    }
}