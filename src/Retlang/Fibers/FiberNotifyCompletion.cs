using System;
using System.Runtime.CompilerServices;

namespace Retlang.Fibers
{
    /// <summary>
    /// A Implementation of INotifyCompletion for IFiber.
    /// </summary>
    public class FiberNotifyCompletion : INotifyCompletion
    {
        private readonly IFiber _fiber;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fiber"></param>
        public FiberNotifyCompletion(IFiber fiber)
        {
            _fiber = fiber;
        }

        /// <summary>
        /// await enabling.
        /// </summary>
        /// <returns></returns>
        public FiberNotifyCompletion GetAwaiter()
        {
            return this;
        }

        /// <summary>
        /// Always false, to have the completion process performed.
        /// </summary>
        public bool IsCompleted { get { return false; } }

        /// <summary>
        /// Called to resume subsequent processing at the end of await.
        /// </summary>
        /// <param name="action"></param>
        public void OnCompleted(Action action)
        {
            _fiber.Enqueue(action);
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void GetResult()
        {}
    }
}
