using System.ComponentModel;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;

namespace WpfExample
{
    ///<summary>
    /// Allows interaction with Windows Forms.  Transparently moves actions onto the Form's thread.
    ///</summary>
    public static class FormFiberCreator
    {
        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <return></return>
        public static IAsyncExecutionContext Create(ISynchronizeInvoke invoker, IExecutor executor)
        {
            return new PoolFiberSlim(new FormAdapter(invoker), executor);
        }
    }
}