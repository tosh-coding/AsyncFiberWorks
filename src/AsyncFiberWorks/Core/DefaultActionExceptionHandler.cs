using System;
using System.Diagnostics;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Default exception handler used when none is supplied.
    /// Logs the exception and suppresses any exceptions thrown by the handler itself.
    /// </summary>
    public sealed class DefaultActionExceptionHandler : IActionExceptionHandler
    {
        public static readonly DefaultActionExceptionHandler Instance = new DefaultActionExceptionHandler();

        private DefaultActionExceptionHandler() { }

        public void Handle(Exception exception)
        {
            if (exception == null) return;
            try
            {
                // Use Trace so it works on .NET Framework and .NET Standard.
                Trace.TraceError("[AsyncFiberWorks] Exception handled: {0}{1}{2}", exception.Message, Environment.NewLine, exception);
            }
            catch
            {
                // Suppress any logging errors.
            }
        }
    }
}