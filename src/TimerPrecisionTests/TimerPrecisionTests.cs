using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;
using AsyncFiberWorks.Windows;
using AsyncFiberWorks.Windows.Timers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TimerPrecisionTests
{
    [TestFixture]
    public class TimerPrecisionTests
    {
        static readonly int maxCount = 30;

        async Task BenchmarkTimerAccuracy(Func<IExecutionContext, Action, IDisposable> timerFunc)
        {
            var timeList = new List<TimeSpan>();
            int counter = 0;
            var fiber = new PoolFiber();
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
                var timer = new IntervalThreadingTimer();
                timer.ScheduleOnInterval(() => fiber.Enqueue(action), 0, 1);
                return timer;
            });
        }

        [Test]
        public async Task ThreadingTimerWithTimeBeginPeriod()
        {
            WinApi.timeBeginPeriod(1);
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var timer = new IntervalThreadingTimer();
                timer.ScheduleOnInterval(() => fiber.Enqueue(action), 0, 1);
                return timer;
            });
            WinApi.timeEndPeriod(1);
        }

        [Test]
        public async Task TaskDelayWithTimeBeginPeriod()
        {
            WinApi.timeBeginPeriod(1);
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
            WinApi.timeEndPeriod(1);
        }

        [Test]
        public async Task ThreadSleepWithTimeBeginPeriod()
        {
            WinApi.timeBeginPeriod(1);
            await BenchmarkThreadSleep();
            WinApi.timeEndPeriod(1);
        }

        async Task BenchmarkThreadSleep()
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var disposable = new Unsubscriber();
                var consumerThread = ConsumerThread.StartNew();
                consumerThread.Enqueue(() =>
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
                    consumerThread.Stop();
                });
                return disposable;
            });
        }

        [Test]
        public async Task ThreadSleepWithNtSetTimerResolution1000us()
        {
            uint targetResolution100ns = 10000;
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadSleep();
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
        }

        [Test]
        public async Task ThreadSleepWithNtSetTimerResolution500us()
        {
            uint targetResolution100ns = 5000;
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadSleep();
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
        }

        [Test]
        public async Task ThreadSleepWithNtSetTimerResolution100us()
        {
            uint targetResolution100ns = 1000;
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadSleep();
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
        }

        [Test]
        public async Task ThreadSleepWithNtSetTimerResolution999us()
        {
            uint targetResolution100ns = 9990;
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadSleep();
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
        }

        [Test]
        public async Task TimeSetEventWithNtSetTimerResolution500us()
        {
            WinApi.timeBeginPeriod(1);
            uint targetResolution100ns = 5000;
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkTimeSetEvent();
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            WinApi.timeEndPeriod(1);
        }

        async Task BenchmarkTimeSetEvent()
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                uint timerID = 0;
                WinApi.TimeCallback teh;
                teh = (UInt32 id, UInt32 msg, ref UInt32 userCtx, UInt32 rsv1, UInt32 rsv2) =>
                {
                    fiber.Enqueue(action);
                };

                uint firstDelayMs = 1;
                uint intervalMs = 1;
                uint userctx = 0;
                uint TIME_PERIODIC = 1;
                timerID = WinApi.timeSetEvent(firstDelayMs, intervalMs, teh, ref userctx, TIME_PERIODIC);

                var disposable = new Unsubscriber(() =>
                {
                    fiber.Enqueue(() =>
                    {
                        if (timerID != 0)
                        {
                            WinApi.timeKillEvent(timerID);
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
            WinApi.timeBeginPeriod(1);
            uint targetResolution100ns = 1000;
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkTimeSetEvent();
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            WinApi.timeEndPeriod(1);
        }

        [Test]
        public async Task TimeSetEventWithNtSetTimerResolution1000us()
        {
            WinApi.timeBeginPeriod(1);
            uint targetResolution100ns = 10000;
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkTimeSetEvent();
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            WinApi.timeEndPeriod(1);
        }

        [Test]
        public async Task TimeSetEventWithTimeBeginPeriod()
        {
            WinApi.timeBeginPeriod(1);
            await BenchmarkTimeSetEvent();
            WinApi.timeEndPeriod(1);
        }

        [Test]
        [TestCase(1000)]
        [TestCase(500)]
        [TestCase(100)]
        public async Task WaitableTimerAndWaitOneWithTimeBeginPeriod(int sleepUs)
        {
            WinApi.timeBeginPeriod(1);
            await BenchmarkWaitableTimerAndWaitOne(sleepUs);
            WinApi.timeEndPeriod(1);
        }

        [Test]
        [TestCase(10000u, 1000)]
        [TestCase(10000u, 500)]
        [TestCase(10000u, 100)]
        //[TestCase(5000u, 1000)]
        //[TestCase(5000u, 500)]
        //[TestCase(5000u, 100)]
        //[TestCase(1000u, 1000)]
        //[TestCase(1000u, 100)]
        public async Task WaitableTimerAndWaitOneWithNtSetTimerResolution(uint targetResolution100ns, int sleepUs)
        {
            WinApi.timeBeginPeriod(1);
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkWaitableTimerAndWaitOne(sleepUs);
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            WinApi.timeEndPeriod(1);
        }

        async Task BenchmarkWaitableTimerAndWaitOne(int sleepUs)
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var disposable = new Unsubscriber();
                var consumerThread = ConsumerThread.StartNew();
                consumerThread.Enqueue(() =>
                {
                    var cancellation = new CancellationTokenSource();
                    disposable.Append(cancellation);

                    var timerWaitHandle = new WaitableTimer();
                    while (true)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            consumerThread.Stop();
                            break;
                        }
                        else
                        {
                            timerWaitHandle.Set(sleepUs * -10L);
                            timerWaitHandle.WaitOne();

                            fiber.Enqueue(action);
                        }
                    }
                    timerWaitHandle.Dispose();
                });
                return disposable;
            });
        }

        [Test]
        [TestCase(1000)]
        [TestCase(500)]
        [TestCase(100)]
        public async Task WaitableTimerHighResolutionAndWaitOneWithTimeBeginPeriod(int sleepUs)
        {
            WinApi.timeBeginPeriod(1);
            await BenchmarkWaitableTimerHighResolutionAndWaitOne(sleepUs);
            WinApi.timeEndPeriod(1);
        }

        [Test]
        [TestCase(10000u, 1000)]
        [TestCase(10000u, 500)]
        [TestCase(10000u, 100)]
        //[TestCase(5000u, 1000)]
        //[TestCase(5000u, 500)]
        //[TestCase(5000u, 100)]
        //[TestCase(1000u, 1000)]
        //[TestCase(1000u, 100)]
        public async Task WaitableTimerHighResolutionAndWaitOneWithNtSetTimerResolution(uint targetResolution100ns, int sleepUs)
        {
            WinApi.timeBeginPeriod(1);
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkWaitableTimerHighResolutionAndWaitOne(sleepUs);
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            WinApi.timeEndPeriod(1);
        }

        [Test]
        [TestCase(1000)]
        [TestCase(500)]
        [TestCase(100)]
        public async Task WaitableTimerAndWaitOneSingle(int sleepUs)
        {
            await BenchmarkWaitableTimerAndWaitOne(sleepUs);
        }

        [Test]
        [TestCase(100u)]
        [TestCase(15u)]
        [TestCase(10u)]
        [TestCase(5u)]
        public async Task WaitableTimerHighResolutionAndWaitOne500usWithTimeBeginPeriod(uint timePeriod)
        {
            WinApi.timeBeginPeriod(timePeriod);
            await BenchmarkWaitableTimerHighResolutionAndWaitOne(500);
            WinApi.timeEndPeriod(timePeriod);
        }

        async Task BenchmarkWaitableTimerHighResolutionAndWaitOne(int sleepUs)
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var disposable = new Unsubscriber();
                var consumerThread = ConsumerThread.StartNew();
                consumerThread.Enqueue(() =>
                {
                    var cancellation = new CancellationTokenSource();
                    disposable.Append(cancellation);

                    var timerWaitHandle = new WaitableTimerEx();
                    while (true)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            consumerThread.Stop();
                            break;
                        }
                        else
                        {
                            timerWaitHandle.Set(sleepUs * -10L);
                            timerWaitHandle.WaitOne();

                            fiber.Enqueue(action);
                        }
                    }
                    timerWaitHandle.Dispose();
                });
                return disposable;
            });
        }

        [Test]
        public async Task ThreadPoolRegisterWaitForSingleObjectTimeoutWithTimeBeginPeriod()
        {
            WinApi.timeBeginPeriod(1);
            await BenchmarkThreadPoolRegisterWaitForSingleObjectTimeout();
            WinApi.timeEndPeriod(1);
        }

        [Test]
        [TestCase(10000u)]
        [TestCase(5000u)]
        [TestCase(1000u)]
        public async Task ThreadPoolRegisterWaitForSingleObjectTimeoutWithNtSetTimerResolution(uint targetResolution100ns)
        {
            WinApi.timeBeginPeriod(1);
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkThreadPoolRegisterWaitForSingleObjectTimeout();
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            WinApi.timeEndPeriod(1);
        }

        async Task BenchmarkThreadPoolRegisterWaitForSingleObjectTimeout()
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var disposable = new Unsubscriber();
                var cancellation = new CancellationTokenSource();
                disposable.Append(cancellation);
                var handleList = new RegisteredWaitHandle[1];

                // Non-operating dummy event.
                var manualResetEvent = new ManualResetEventSlim(false, 0);

                // Timeout at 1 ms, repeat.
                int timeoutMs = 1;
                bool executeOnlyOnce = false;

                var tmpHandle = ThreadPool.RegisterWaitForSingleObject(manualResetEvent.WaitHandle, (state, timeout) =>
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        var handle = handleList[0];
                        handle.Unregister(null);
                        manualResetEvent.Dispose();
                    }
                    else
                    {
                        fiber.Enqueue(action);
                    }
                }, null, timeoutMs, executeOnlyOnce);
                handleList[0] = tmpHandle;
                return disposable;
            });
        }

        [Test]
        [TestCase(1000)]
        [TestCase(500)]
        [TestCase(100)]
        public async Task WaitableTimerAndThreadPoolRegisterWaitForSingleObjectWithTimeBeginPeriod(int sleepUs)
        {
            WinApi.timeBeginPeriod(1);
            await BenchmarkWaitableTimerAndThreadPoolRegisterWaitForSingleObject(sleepUs);
            WinApi.timeEndPeriod(1);
        }

        [Test]
        [TestCase(10000u, 1000)]
        [TestCase(10000u, 500)]
        [TestCase(10000u, 100)]
        //[TestCase(5000u, 1000)]
        //[TestCase(5000u, 500)]
        //[TestCase(5000u, 100)]
        //[TestCase(1000u, 1000)]
        //[TestCase(1000u, 100)]
        public async Task WaitableTimerAndThreadPoolRegisterWaitForSingleObjectWithNtSetTimerResolution(uint targetResolution100ns, int sleepUs)
        {
            WinApi.timeBeginPeriod(1);
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkWaitableTimerAndThreadPoolRegisterWaitForSingleObject(sleepUs);
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            WinApi.timeEndPeriod(1);
        }

        async Task BenchmarkWaitableTimerAndThreadPoolRegisterWaitForSingleObject(int sleepUs)
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var disposable = new Unsubscriber();
                var cancellation = new CancellationTokenSource();
                disposable.Append(cancellation);
                Action[] actionList = new Action[1];
                var handleList = new RegisteredWaitHandle[1];

                var waitableTimer = new WaitableTimer(manualReset: false);
                int timeoutMs = Timeout.Infinite;
                bool executeOnlyOnce = false;

                waitableTimer.Set(sleepUs * -10L);
                var tmpHandle = ThreadPool.RegisterWaitForSingleObject(waitableTimer, (state, timeout) =>
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        var handle = handleList[0];
                        handle.Unregister(null);
                        waitableTimer.Dispose();
                    }
                    else
                    {
                        fiber.Enqueue(action);
                        waitableTimer.Set(sleepUs * -10L);
                    }
                }, null, timeoutMs, executeOnlyOnce);
                handleList[0] = tmpHandle;
                return disposable;
            });
        }

        [Test]
        [TestCase(1000)]
        [TestCase(500)]
        [TestCase(100)]
        public async Task WaitableTimerHighResolutionAndThreadPoolRegisterWaitForSingleObjectWithTimeBeginPeriod(int sleepUs)
        {
            WinApi.timeBeginPeriod(1);
            await BenchmarkWaitableTimerHighResolutionAndThreadPoolRegisterWaitForSingleObject(sleepUs);
            WinApi.timeEndPeriod(1);
        }

        [Test]
        [TestCase(10000u, 1000)]
        [TestCase(10000u, 500)]
        [TestCase(10000u, 100)]
        //[TestCase(5000u, 1000)]
        //[TestCase(5000u, 500)]
        //[TestCase(5000u, 100)]
        //[TestCase(1000u, 1000)]
        //[TestCase(1000u, 500)]
        //[TestCase(1000u, 100)]
        public async Task WaitableTimerHighResolutionAndThreadPoolRegisterWaitForSingleObjectWithNtSetTimerResolution(uint targetResolution100ns, int sleepUs)
        {
            WinApi.timeBeginPeriod(1);
            uint currentResolution = 0;
            WinApi.NtSetTimerResolution(targetResolution100ns, true, ref currentResolution);
            await BenchmarkWaitableTimerHighResolutionAndThreadPoolRegisterWaitForSingleObject(sleepUs);
            WinApi.NtSetTimerResolution(targetResolution100ns, false, ref currentResolution);
            WinApi.timeEndPeriod(1);
        }

        async Task BenchmarkWaitableTimerHighResolutionAndThreadPoolRegisterWaitForSingleObject(int sleepUs)
        {
            await BenchmarkTimerAccuracy((fiber, action) =>
            {
                var disposable = new Unsubscriber();
                var cancellation = new CancellationTokenSource();
                disposable.Append(cancellation);
                Action[] actionList = new Action[1];
                var handleList = new RegisteredWaitHandle[1];

                var waitableTimer = new WaitableTimerEx(manualReset: false);
                int timeoutMs = Timeout.Infinite;
                bool executeOnlyOnce = false;

                waitableTimer.Set(sleepUs * -10L);
                var tmpHandle = ThreadPool.RegisterWaitForSingleObject(waitableTimer, (state, timeout) =>
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        var handle = handleList[0];
                        handle.Unregister(null);
                        waitableTimer.Dispose();
                    }
                    else
                    {
                        fiber.Enqueue(action);
                        waitableTimer.Set(sleepUs * -10L);
                    }
                }, null, timeoutMs, executeOnlyOnce);
                handleList[0] = tmpHandle;
                return disposable;
            });
        }
    }
}
