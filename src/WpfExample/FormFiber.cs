using System.ComponentModel;
using Retlang.Core;
using Retlang.Fibers;

namespace WpfExample
{
    ///<summary>
    /// Allows interaction with Windows Forms.  Transparently moves actions onto the Form's thread.
    ///</summary>
    public class FormFiber : FiberWithDisposableList
    {
        /// <summary>
        /// Creates an instance.
        /// </summary>
        public FormFiber(ISynchronizeInvoke invoker, IExecutor executor)
            : base(new PoolFiberSlim(new FormAdapter(invoker), executor), new Subscriptions())
        {
        }

        /// <summary>
        /// Clears all subscriptions, scheduled.
        /// </summary>
        public void Stop()
        {
            base.Dispose();
        }
    }
}