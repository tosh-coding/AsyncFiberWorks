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
        private readonly GuiFiberSlim _guiFiberSlim;

        /// <summary>
        /// Creates an instance.
        /// </summary>
        public FormFiber(ISynchronizeInvoke invoker, IExecutor executor)
            : this(new GuiFiberSlim(new FormAdapter(invoker), executor))
        {
        }

        /// <summary>
        /// Create a new instance and call the Start method.
        /// </summary>
        /// <param name="invoker"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public static FormFiber StartNew(ISynchronizeInvoke invoker, IExecutor executor)
        {
            var fiber = new FormFiber(invoker, executor);
            fiber.Start();
            return fiber;
        }

        private FormFiber(GuiFiberSlim guiFiberSlim)
            : base(guiFiberSlim)
        {
            _guiFiberSlim = guiFiberSlim;
        }

        /// <summary>
        /// Stops the fiber.
        /// </summary>
        public void Stop()
        {
            base.Dispose();
        }
    }
}