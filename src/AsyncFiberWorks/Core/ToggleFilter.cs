namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// A filter that can be toggled to run or skip.
    /// </summary>
    public class ToggleFilter
    {
        private bool _running = true;

        /// <summary>
        /// When disabled, actions will be ignored by filter.
        /// The filter is typically disabled at shutdown
        /// to prevent any pending actions from being executed.
        /// </summary>
        public bool IsEnabled
        {
            get { return _running; }
            set { _running = value; }
        }
    }
}