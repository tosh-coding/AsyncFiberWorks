using System;
using System.Threading;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Another implementation that does not use the .NET Thread Pool.
    /// </summary>
    public class AnotherThreadPool : IThreadPool
    {
        private static object _lockObjStatic = new object();
        private static AnotherThreadPool _instance = null;

        private UserThreadPool _userThreadPool = null;

        /// <summary>
        /// The singleton instance of AnotherThreadPool.
        /// </summary>
        public static AnotherThreadPool Instance
        {
            get
            {
                lock (_lockObjStatic)
                {
                    if (_instance == null)
                    {
                        _instance = new AnotherThreadPool();
                    }
                    return _instance;
                }
            }
        }

        private AnotherThreadPool()
        {
            _userThreadPool = UserThreadPool.StartNew();
        }

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="callback"></param>
        public void Queue(WaitCallback callback)
        {
            _userThreadPool.Queue((x) =>
            {
                try
                {
                    callback(x);
                }
                catch (Exception) { }
            });
        }
    }
}
