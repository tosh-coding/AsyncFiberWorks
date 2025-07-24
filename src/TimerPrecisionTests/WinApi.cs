using System.Runtime.InteropServices;

namespace TimerPrecisionTests
{
    /// <summary>
    /// Windows APIs.
    /// </summary>
    public static class WinApi
    {
        /// <summary>
        /// Delegate for timeSetEvent.
        /// </summary>
        /// <param name="timerId"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <param name="reserved1"></param>
        /// <param name="reserved2"></param>
        public delegate void TimeCallback(uint timerId, uint msg, ref uint user, uint reserved1, uint reserved2);

        /// <summary>
        /// timeBeginPeriod.
        /// </summary>
        /// <param name="period"></param>
        /// <returns></returns>
        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);

        /// <summary>
        /// timeEndPeriod.
        /// </summary>
        /// <param name="period"></param>
        /// <returns></returns>
        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);

        /// <summary>
        /// NtSetTimerResolution.
        /// </summary>
        /// <param name="DesiredResolution"></param>
        /// <param name="SetResolution"></param>
        /// <param name="CurrentResolution"></param>
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);

        /// <summary>
        /// timeSetEvent.
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="resolution"></param>
        /// <param name="timeProc"></param>
        /// <param name="user"></param>
        /// <param name="eventType"></param>
        /// <returns></returns>
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint timeSetEvent(uint delay, uint resolution, TimeCallback timeProc, ref uint user, uint eventType);

        /// <summary>
        /// timeKillEvent.
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint timeKillEvent(uint timerId);
    }
}
