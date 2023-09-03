using System;
using System.Runtime.CompilerServices;

namespace Retlang.Fibers
{
    public class FiberNotifyCompletion : INotifyCompletion
    {
        private readonly IFiber _fiber;

        public FiberNotifyCompletion(IFiber fiber)
        {
            _fiber = fiber;
        }

        public FiberNotifyCompletion GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted { get { return false; } }

        public void OnCompleted(Action action)
        {
            _fiber.Enqueue(action);
        }

        public void GetResult()
        {}
    }
}
