using System;
using Retlang.Core;

namespace Retlang.Fibers
{
    ///<summary>
    /// Allows interaction with Windows Forms.  Transparently moves actions onto the Form's thread.
    ///</summary>
    public class GuiFiber : FiberWithDisposableList
    {
        private readonly GuiFiberSlim _guiFiberSlim;

        /// <summary>
        /// Creates an instance.
        /// </summary>
        public GuiFiber(IExecutionContext executionContext, IExecutor executor)
            : this(new GuiFiberSlim(executionContext, executor))
        {
        }

        private GuiFiber(GuiFiberSlim guiFiberSlim)
            : base(guiFiberSlim)
        {
            _guiFiberSlim = guiFiberSlim;
        }

        /// <summary>
        /// <see cref="IFiber.Start()"/>
        /// </summary>
        public override void Start()
        {
            base.Start();
        }

        /// <summary>
        /// <see cref="IDisposable.Dispose()"/>
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
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