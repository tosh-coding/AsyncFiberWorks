using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class TimerPrecisionTests
    {
        static readonly int maxCount = 30;

        async Task BenchmarkTimerAccuracy(Func<IExecutionContext, Action, IDisposable> timerFunc)
        {
            var timeList = new List<TimeSpan>();
            int counter = 0;
            var fiber = new PoolFiberSlim();
            var tcs = new TaskCompletionSource<int>();
            var sw = Stopwatch.StartNew();
            Action action = () =>
            {
                if (counter < maxCount)
                {
                    timeList.Add(sw.Elapsed);
                }
                else if (counter == maxCount)
                {
                    tcs.SetResult(0);
                }
                else
                {
                }
                counter += 1;
            };

            var timerDisposable = timerFunc(fiber, action);

            await tcs.Task;
            timerDisposable.Dispose();
            WriteTimeList(timeList);
        }

        void WriteTimeList(List<TimeSpan> timeList)
        {
            TimeSpan prevT = timeList[0];
            foreach (var t in timeList)
            {
                Console.WriteLine($"{t} - {(t - prevT).TotalMilliseconds}ms");
                prevT = t;
            }
        }

        [Test]
        public async Task ThreadingTimerSingle()
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                return fiber.ScheduleOnInterval(action, 0, 1);
            });
        }

#if NETFRAMEWORK || WINDOWS
        [Test]
        public async Task ThreadingTimerWithTimeBeginPeriod()
        {
            PerfSettings.timeBeginPeriod(1);
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                return fiber.ScheduleOnInterval(action, 0, 1);
            });
            PerfSettings.timeEndPeriod(1);
        }

        [Test]
        public async Task TaskDelayWithTimeBeginPeriod()
        {
            PerfSettings.timeBeginPeriod(1);
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var disposable = new Unsubscriber();
                Task.Run(async () =>
                {
                    var cancellation = new CancellationTokenSource();
                    disposable.Append(cancellation);
                    while (true)
                    {
                        fiber.Enqueue(action);
                        await Task.Delay(1).ConfigureAwait(false);
                        if (cancellation.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                });
                return disposable;
            });
            PerfSettings.timeEndPeriod(1);
        }

        [Test]
        public async Task ThreadSleepWithTimeBeginPeriod()
        {
            PerfSettings.timeBeginPeriod(1);
            await BenchmarkThreadSleep();
            PerfSettings.timeEndPeriod(1);
        }

        async Task BenchmarkThreadSleep()
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var disposable = new Unsubscriber();
                var threadFiber = new ThreadFiber();
                threadFiber.Enqueue(() =>
                {
                    var cancellation = new CancellationTokenSource();
                    disposable.Append(cancellation);
                    while (true)
                    {
                        fiber.Enqueue(action);
                        Thread.Sleep(1);
                        if (cancellation.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    threadFiber.Stop();
                });
                return disposable;
            });
        }

        [Test]
        public async Task ThreadSleepWithNtSetTimerResolution1000us()
        {
            uint targetResolution100ns = 10000;
            uint currentResolution = 0;
            PerfSettings.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadSleep();
            PerfSettings.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
        }

        [Test]
        public async Task ThreadSleepWithNtSetTimerResolution500us()
        {
            uint targetResolution100ns = 5000;
            uint currentResolution = 0;
            PerfSettings.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadSleep();
            PerfSettings.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
        }

        [Test]
        public async Task ThreadSleepWithNtSetTimerResolution100us()
        {
            uint targetResolution100ns = 1000;
            uint currentResolution = 0;
            PerfSettings.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadSleep();
            PerfSettings.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
        }

        [Test]
        public async Task ThreadSleepWithNtSetTimerResolution999us()
        {
            uint targetResolution100ns = 9990;
            uint currentResolution = 0;
            PerfSettings.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadSleep();
            PerfSettings.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
        }

        [Test]
        public async Task TimeSetEventWithNtSetTimerResolution500us()
        {
            PerfSettings.timeBeginPeriod(1);
            uint targetResolution100ns = 5000;
            uint currentResolution = 0;
            PerfSettings.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkTimeSetEvent();
            PerfSettings.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            PerfSettings.timeEndPeriod(1);
        }

        async Task BenchmarkTimeSetEvent()
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                uint timerID = 0;
                PerfSettings.TimeCallback teh;
                teh = (UInt32 id, UInt32 msg, ref UInt32 userCtx, UInt32 rsv1, UInt32 rsv2) =>
                {
                    fiber.Enqueue(action);
                };

                uint firstDelayMs = 1;
                uint intervalMs = 1;
                uint userctx = 0;
                uint TIME_PERIODIC = 1;
                timerID = PerfSettings.timeSetEvent(firstDelayMs, intervalMs, teh, ref userctx, TIME_PERIODIC);

                var disposable = new Unsubscriber(() =>
                {
                    fiber.Enqueue(() =>
                    {
                        if (timerID != 0)
                        {
                            PerfSettings.timeKillEvent(timerID);
                            timerID = 0;
                        }
                    });
                });
                return disposable;
            });
        }

        [Test]
        public async Task TimeSetEventWithNtSetTimerResolution100us()
        {
            PerfSettings.timeBeginPeriod(1);
            uint targetResolution100ns = 1000;
            uint currentResolution = 0;
            PerfSettings.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkTimeSetEvent();
            PerfSettings.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            PerfSettings.timeEndPeriod(1);
        }

        [Test]
        public async Task TimeSetEventWithNtSetTimerResolution1000us()
        {
            PerfSettings.timeBeginPeriod(1);
            uint targetResolution100ns = 10000;
            uint currentResolution = 0;
            PerfSettings.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkTimeSetEvent();
            PerfSettings.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            PerfSettings.timeEndPeriod(1);
        }

        [Test]
        public async Task TimeSetEventWithTimeBeginPeriod()
        {
            PerfSettings.timeBeginPeriod(1);
            await BenchmarkTimeSetEvent();
            PerfSettings.timeEndPeriod(1);
        }
#endif
    }
}
