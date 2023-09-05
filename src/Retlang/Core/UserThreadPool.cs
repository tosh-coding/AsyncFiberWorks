using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Retlang.Core
{
    /// <summary>
    /// Another thread pool implementation.
    /// </summary>
    public class UserThreadPool : IThreadPool, IDisposable
    {
        private static int POOL_COUNT = 0;
        private readonly string _poolName;
        private readonly BlockingCollection<Action> _actions = new BlockingCollection<Action>();
        private Thread[] _threadList = null;
        private long _executionStateLong;

        /// <summary>
        /// Create a thread pool with the default number of worker threads.
        /// </summary>
        public UserThreadPool()
            : this(2)
        {}

        /// <summary>
        /// Create a thread pool.
        /// </summary>
        /// <param name="numberOfThread"></param>
        /// <param name="poolName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        /// <exception cref="ArgumentOutOfRangeException">The numberOfThread must be at least 1.</exception>
        public UserThreadPool(int numberOfThread, string poolName = null, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            if (numberOfThread <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfThread));
            }
            if (poolName == null)
            {
                poolName = "UserThreadPool" + GetNextPoolId();
            }
            _poolName = poolName;
            ExecutionState = ExecutionStateEnum.Created;

            _threadList = new Thread[numberOfThread];
            for (int i = 0; i < numberOfThread; i++)
            {
                var th = new Thread(new ThreadStart(RunThread));
                th.Name = poolName + "-" + i;
                th.IsBackground = isBackground;
                th.Priority = priority;
                _threadList[i] = th;
            }
        }

        /// <summary>
        /// Create a new instance and call the Start method.
        /// </summary>
        /// <param name="numberOfThread"></param>
        /// <param name="poolName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static UserThreadPool StartNew(int numberOfThread = 2, string poolName = null, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            var pool = new UserThreadPool(numberOfThread, poolName, isBackground, priority);
            pool.Start();
            return pool;
        }

        /// <summary>
        /// The dedicated worker threads.
        /// </summary>
        public Thread[] ThreadList
        {
            get { return _threadList; }
        }

        /// <summary>
        /// The pool name.
        /// </summary>
        public string PoolName
        {
            get { return _poolName; }
        }

        private ExecutionStateEnum ExecutionState
        {
            get { return (ExecutionStateEnum)Interlocked.Read(ref _executionStateLong); }
            set { Interlocked.Exchange(ref _executionStateLong, (long)value); }
        }

        private static int GetNextPoolId()
        {
            return Interlocked.Increment(ref POOL_COUNT);
        }

        private void RunThread()
        {
            while (ExecutionState == ExecutionStateEnum.Running)
            {
                var act = _actions.Take();
                act();
            }
        }

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="callback"></param>
        public void Queue(WaitCallback callback)
        {
            Enqueue(() => callback(null));
        }

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _actions.Add(action);
        }

        /// <summary>
        /// Start the threads.
        /// </summary>
        public void Start()
        {
            if (ExecutionState != ExecutionStateEnum.Created)
            {
                throw new ThreadStateException("Already Started");
            }

            ExecutionState = ExecutionStateEnum.Running;
            for (int i = 0; i < _threadList.Length; i++)
            {
                _threadList[i].Start();
            }
        }

        /// <summary>
        /// Stop the threads.
        /// </summary>
        public void Stop()
        {
            if (ExecutionState == ExecutionStateEnum.Created)
            {
                ExecutionState = ExecutionStateEnum.Stopped;
            }
            else if (ExecutionState == ExecutionStateEnum.Running)
            {
                ExecutionState = ExecutionStateEnum.Stopped;
                for (int i = 0; i < _threadList.Length; i++)
                {
                    Enqueue(() => { });
                }
            }
            else if (ExecutionState == ExecutionStateEnum.Stopped)
            {
                // Already stopped.
            }
            else
            {
                // Unknown state.
            }
        }

        ///<summary>
        /// Call join on the threads.
        ///</summary>
        public void Join()
        {
            for (int i = 0; i < _threadList.Length; i++)
            {
                _threadList[i].Join();
            }
        }

        /// <summary>
        /// Stop the threads.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        ///<summary>
        /// UserThreadPool execution state.
        ///</summary>
        private enum ExecutionStateEnum
        {
            ///<summary>
            /// Created but not running
            ///</summary>
            Created,
            ///<summary>
            /// After start
            ///</summary>
            Running,
            ///<summary>
            /// After stopped
            ///</summary>
            Stopped
        }
    }
}