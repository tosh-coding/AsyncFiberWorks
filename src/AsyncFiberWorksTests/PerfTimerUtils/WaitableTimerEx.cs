#if NETFRAMEWORK || WINDOWS
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace AsyncFiberWorksTests.Perf
{
    public class WaitableTimerEx : WaitHandle
    {
        private const uint TIMER_ALL_ACCESS = 0x1F0003;
        private const uint CREATE_WAITABLE_TIMER_MANUAL_RESET = 0x00000001;
        private const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x00000002;

        [DllImport("kernel32.dll")]
        public static extern SafeWaitHandle CreateWaitableTimerEx(IntPtr lpTimerAttributes, string lpTimerName, uint dwFlags, uint dwDesiredAccess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimerEx(SafeWaitHandle hTimer, [In] ref long pDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, IntPtr WakeContext, uint TolerableDelay);

        public WaitableTimerEx(bool manualReset = true, bool highResolution = true, string timerName = null)
        {
            uint flags = 0;
            flags |= manualReset ? CREATE_WAITABLE_TIMER_MANUAL_RESET : 0;
            flags |= highResolution ? CREATE_WAITABLE_TIMER_HIGH_RESOLUTION : 0;
            SafeWaitHandle = CreateWaitableTimerEx(IntPtr.Zero, timerName, flags, TIMER_ALL_ACCESS);
        }

        public void Set(long dueTime, int period = 0)
        {
            if (!SetWaitableTimerEx(SafeWaitHandle, ref dueTime, period, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0))
            //if (!WaitableTimer.SetWaitableTimer(SafeWaitHandle, ref dueTime, period, IntPtr.Zero, IntPtr.Zero, false))
            {
                //int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception();
            }
        }
    }
}
#endif
