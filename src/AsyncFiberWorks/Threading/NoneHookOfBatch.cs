using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Placeholder class that does nothing.
    /// </summary>
    public class NoneHookOfBatch : IHookOfBatch
    {
        /// <summary>
        /// Singleton instances.
        /// This class has no members, so it can be shared.
        /// </summary>
        public static readonly NoneHookOfBatch Instance = new NoneHookOfBatch();

        /// <summary>
        /// Nothing to do.
        /// </summary>
        /// <param name="numberOfActions"></param>
        public void OnBeforeExecute(int numberOfActions) { }

        /// <summary>
        /// Nothing to do.
        /// </summary>
        /// <param name="numberOfActions"></param>
        public void OnAfterExecute(int numberOfActions) { }
    }
}
