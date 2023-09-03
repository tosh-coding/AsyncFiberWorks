using System;
using System.Runtime.CompilerServices;

namespace Retlang.Fibers
{
    public class FiberSlimNotifyCompletion : INotifyCompletion
    {
        private readonly IFiberSlim _fiber;

        public FiberSlimNotifyCompletion(IFiberSlim fiber)
        {
            _fiber = fiber;
        }

        public FiberSlimNotifyCompletion GetAwaiter()
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
