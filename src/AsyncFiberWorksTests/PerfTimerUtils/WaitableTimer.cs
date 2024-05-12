#if NETFRAMEWORK || WINDOWS
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace AsyncFiberWorksTests.Perf
{
    /// <summary>
    /// Quoted from https://stackoverflow.com/questions/15858751/analog-to-waitable-timers-in-net
    /// </summary>
    public class WaitableTimer : WaitHandle
    {
        [DllImport("kernel32.dll")]
        public static extern SafeWaitHandle CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimer(SafeWaitHandle hTimer, [In] ref long pDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, [MarshalAs(UnmanagedType.Bool)] bool fResume);

        public WaitableTimer(bool manualReset = true, string timerName = null)
        {
            SafeWaitHandle = CreateWaitableTimer(IntPtr.Zero, manualReset, timerName);
        }

        public void Set(long dueTime, int period = 0)
        {
            if (!SetWaitableTimer(SafeWaitHandle, ref dueTime, period, IntPtr.Zero, IntPtr.Zero, false))
            {
                throw new Win32Exception();
            }
        }
    }
}
#endif
