namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Hooks at the timing before and after the execution of the pending action.
    /// </summary>
    public interface IHookOfBatch
    {
        /// <summary>
        /// Callbacks to be called before batch execution.
        /// </summary>
        /// <param name="numberOfActions">Number of actions in the batch.</param>
        void OnBeforeExecute(int numberOfActions);

        /// <summary>
        /// Callbacks to be called after batch execution.
        /// </summary>
        void OnAfterExecute(int numberOfActions);
    }
}