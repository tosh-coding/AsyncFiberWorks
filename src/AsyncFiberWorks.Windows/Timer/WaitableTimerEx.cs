using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace AsyncFiberWorks.Windows.Timer
{
    /// <summary>
    /// Waitable timer with CREATE_WAITABLE_TIMER_HIGH_RESOLUTION.
    /// </summary>
    public class WaitableTimerEx : WaitHandle
    {
        private const uint TIMER_ALL_ACCESS = 0x1F0003;
        private const uint CREATE_WAITABLE_TIMER_MANUAL_RESET = 0x00000001;
        private const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeWaitHandle CreateWaitableTimerEx(IntPtr lpTimerAttributes, string lpTimerName, uint dwFlags, uint dwDesiredAccess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWaitableTimerEx(SafeWaitHandle hTimer, [In] ref long pDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, IntPtr WakeContext, uint TolerableDelay);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CancelWaitableTimer(SafeWaitHandle hTimer);

        /// <summary>
        /// Creates a high resolution timer.
        /// see https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-createwaitabletimerexw
        /// </summary>
        /// <param name="manualReset">If it is a manual reset, the signal state is not reset until Set is called. Otherwise, it is auto-reset without a call to Set.</param>
        /// <param name="timerName">A timer name.</param>
        /// <exception cref="Win32Exception"></exception>
        public WaitableTimerEx(bool manualReset = true, string timerName = null)
        {
            uint flags = 0;
            flags |= manualReset ? CREATE_WAITABLE_TIMER_MANUAL_RESET : 0;
            flags |= CREATE_WAITABLE_TIMER_HIGH_RESOLUTION;
            SafeWaitHandle = CreateWaitableTimerEx(IntPtr.Zero, timerName, flags, TIMER_ALL_ACCESS);
            if (SafeWaitHandle == null)
            {
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// Activate the timer.
        /// see https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-setwaitabletimerex
        /// </summary>
        /// <param name="dueTime">Initial wait time for timer. 100 nanosecond units.</param>
        /// <param name="period">The interval at which the timer expires. In milliseconds. If this value is 0, the timer will expire only once; if it is greater than 0, it will expire periodically and continue until canceled.</param>
        /// <exception cref="Win32Exception"></exception>
        public void Set(long dueTime, int period = 0)
        {
            if (!SetWaitableTimerEx(SafeWaitHandle, ref dueTime, period, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0))
            {
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// Deactivate the timer.
        /// see https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-cancelwaitabletimer
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool Cancel()
        {
            return CancelWaitableTimer(SafeWaitHandle);
        }
    }
}
