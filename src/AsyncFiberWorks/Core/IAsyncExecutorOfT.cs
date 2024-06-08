using System;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Executes a task.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncExecutor<T>
    {
        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <param name="func">A function that returns a task.</param>
        /// <returns>A task that waits for the task to be completed.</returns>
        Task Execute(T arg, Func<T, Task> func);
    }
}
