#if NETFRAMEWORK || WINDOWS
using System.Runtime.InteropServices;

namespace AsyncFiberWorksTests
{
    public struct TIMECAPS
    {
        public uint PeriodMin;
        public uint PeriodMax;
    }

    public static class PerfSettings
    {
        public delegate void TimeCallback(uint timerId, uint msg, ref uint user, uint reserved1, uint reserved2);

        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);

        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint timeSetEvent(uint delay, uint resolution, TimeCallback timeProc, ref uint user, uint eventType);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint timeKillEvent(uint timerId);
    }
}
#endif
