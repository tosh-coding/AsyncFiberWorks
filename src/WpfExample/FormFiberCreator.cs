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
        /// <param name="invoker"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public static IFiber Create(ISynchronizeInvoke invoker, IActionExceptionHandler exceptionHandler = null)
        {
            return new PoolFiber(new FormAdapter(invoker), exceptionHandler);
        }
    }
}