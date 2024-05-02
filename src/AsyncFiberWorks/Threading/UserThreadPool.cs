using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Another thread pool implementation.
    /// </summary>
    public class UserThreadPool : IThreadPool, IDisposable
    {
        private static int POOL_COUNT = 0;
        private readonly string _poolName;
        private readonly IDedicatedConsumerThreadPool _queue;
        private UserWorkerThread[] _threadList = null;
        private long _executionStateLong;

        /// <summary>
        /// Create a thread pool with the default number of worker threads.
        /// </summary>
        public static UserThreadPool Create(int numberOfThread = 1, string poolName = null, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            return new UserThreadPool(numberOfThread, poolName, isBackground, priority);
        }

        /// <summary>
        /// Create a thread pool.
        /// </summary>
        /// <param name="numberOfThread"></param>
        /// <param name="poolName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        /// <exception cref="ArgumentOutOfRangeException">The numberOfThread must be at least 1.</exception>
        public UserThreadPool(int numberOfThread = 1, string poolName = null, bool isBackground = true, ThreadPriority priority = ThreadPriority.Normal)
        {
            if (numberOfThread < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfThread));
            }
            if (poolName == null)
            {
                poolName = "UserThreadPool" + GetNextPoolId();
            }

            var queue = new SharedBlockingCollectionQueue(numberOfThread);
            _poolName = poolName;
            _queue = queue;
            ExecutionState = ExecutionStateEnum.Created;

            var works = queue.Works;
            _threadList = new UserWorkerThread[works.Length];
            for (int i = 0; i < _threadList.Length; i++)
            {
                string threadName = poolName + "-" + i;
                var th = new UserWorkerThread(works[i], threadName, isBackground, priority);
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
            var pool = UserThreadPool.Create(numberOfThread, poolName, isBackground, priority);
            pool.Start();
            return pool;
        }

        /// <summary>
        /// The dedicated worker threads.
        /// </summary>
        public Thread[] ThreadList
        {
            get { return _threadList.Select(x => x.Thread).ToArray(); }
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

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="callback"></param>
        public void Queue(WaitCallback callback)
        {
            _queue.Queue(callback);
        }

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Queue((x) => action());
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
                    _threadList[i].Stop();
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
        public Task Join()
        {
            return Task.WhenAll(_threadList.Select(x => x.Join()).ToArray());
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