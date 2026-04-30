using AsyncFiberWorks.Core;
using System;

namespace AsyncFiberWorks.Fibers
{
    /// <summary>
    /// Lightweight proxy implementing <see cref="IFiber"/> that forwards enqueued work
    /// to a <see cref="KeyedPoolFiber"/> instance for a specific integer key.
    /// </summary>
    /// <remarks>
    /// This proxy does not perform any scheduling itself — it simply delegates calls
    /// to the owning <see cref="KeyedPoolFiber"/> which is responsible for creating
    /// or locating the correct <see cref="PoolFiber"/> for the key and managing its lifecycle.
    /// The proxy is internal and intended to be returned by <see cref="KeyedPoolFiber.CreateFiber(int)"/>.
    /// </remarks>
    internal class KeyedPoolFiberProxy : IFiber
    {
        private readonly int _key;
        private readonly KeyedPoolFiber _owner;

        internal KeyedPoolFiberProxy(int key, KeyedPoolFiber owner)
        {
            _key = key;
            _owner = owner;
        }

        /// <summary>
        /// Enqueue a parameterless action on the keyed fiber.
        /// The action will be executed serially with other actions for the same key.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        public void Enqueue(Action action)
        {
            _owner.EnqueueKeyed(_key, action);
        }

        /// <summary>
        /// Enqueue an action that receives <see cref="IFiberExecutionEventArgs"/> on the keyed fiber.
        /// The owner will decrement internal counters when the action completes.
        /// </summary>
        /// <param name="action">Action that accepts execution event arguments.</param>
        public void Enqueue(Action<IFiberExecutionEventArgs> action)
        {
            _owner.EnqueueKeyed(_key, action);
        }
    }
}
