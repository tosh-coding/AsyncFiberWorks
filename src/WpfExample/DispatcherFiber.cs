﻿using System.Windows.Threading;
using Retlang.Core;

namespace WpfExample
{
    /// <summary>
    /// Adapts Dispatcher to a Fiber. Transparently moves actions onto the Dispatcher thread.
    /// </summary>
    public class DispatcherFiber : GuiFiber
    {
        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="priority">The priority.</param>
        /// <param name="executor">The executor.</param>
        public DispatcherFiber(Dispatcher dispatcher, DispatcherPriority priority, IExecutor executor)
            : base(new DispatcherAdapter(dispatcher, priority), executor)
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="executor">The priority.</param>
        public DispatcherFiber(Dispatcher dispatcher, IExecutor executor)
            : this(dispatcher, DispatcherPriority.Normal, executor)
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="priority">The priority.</param>
        public DispatcherFiber(Dispatcher dispatcher, DispatcherPriority priority)
            : this(dispatcher, priority, new DefaultExecutor())
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        public DispatcherFiber(Dispatcher dispatcher)
            : this(dispatcher, new DefaultExecutor())
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread of the
        /// current dispatcher.
        /// </summary>
        /// <param name="priority">The priority.</param>
        public DispatcherFiber(DispatcherPriority priority)
            : this(Dispatcher.CurrentDispatcher, priority, new DefaultExecutor())
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread of the
        /// current dispatcher.
        /// </summary>
        public DispatcherFiber()
            : this(Dispatcher.CurrentDispatcher, new DefaultExecutor())
        {
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread, and call the Start method.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="priority"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public static DispatcherFiber StartNew(Dispatcher dispatcher, DispatcherPriority priority, IExecutor executor)
        {
            var fiber = new DispatcherFiber(dispatcher, priority, executor);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread, and call the Start method.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public static DispatcherFiber StartNew(Dispatcher dispatcher, IExecutor executor)
        {
            var fiber = new DispatcherFiber(dispatcher, executor);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread, and call the Start method.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static DispatcherFiber StartNew(Dispatcher dispatcher, DispatcherPriority priority)
        {
            var fiber = new DispatcherFiber(dispatcher, priority);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread, and call the Start method.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public static DispatcherFiber StartNew(Dispatcher dispatcher)
        {
            var fiber = new DispatcherFiber(dispatcher);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread of the
        /// current dispatcher, and call the Start method.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static DispatcherFiber StartNew(DispatcherPriority priority)
        {
            var fiber = new DispatcherFiber(priority);
            fiber.Start();
            return fiber;
        }

        /// <summary>
        /// Constructs a Fiber that executes on dispatcher thread of the
        /// current dispatcher.
        /// </summary>
        /// <returns></returns>
        public static DispatcherFiber StartNew()
        {
            var fiber = new DispatcherFiber();
            fiber.Start();
            return fiber;
        }
    }
}