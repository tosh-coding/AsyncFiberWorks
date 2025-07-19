using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Arguments and results of processing.
    /// </summary>
    public class ProcessedFlagEventArgs<T> : EventArgs
    {
        /// <summary>
        /// An argument.
        /// </summary>
        public T Arg { get; set; }

        /// <summary>
        /// Indicates whether it has been processed.
        /// </summary>
        public bool Processed { get; set; }
    }
}
