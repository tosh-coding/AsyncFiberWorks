using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Subscribes for an initial snapshot and then incremental update.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SnapshotRequest<T> : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Channel<bool> _control;
        private readonly Channel<T> _receive;

        private IDisposable _reply;
        private bool _disposed = false;
        private IDisposable _disposableOfReceiver;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="receive"></param>
        public SnapshotRequest(Channel<bool> control, Channel<T> receive)
        {
            _control = control;
            _receive = receive;
        }

        public void StartSubscribe(Channel<IRequest<Channel<T>, IDisposable>> requestChannel)
        {
            var replyChannel = new Channel<IDisposable>();
            _reply = replyChannel.Subscribe((disposableOfReceiver) => DefaultThreadPool.Instance.Queue((_) =>
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        disposableOfReceiver.Dispose();
                        return;
                    }

                    _reply.Dispose();
                    _reply = null;
                    _disposableOfReceiver = disposableOfReceiver;
                    _control.Publish(false);
                }
            }));
            requestChannel.Publish(new RequestReplyChannelRequest<Channel<T>, IDisposable>(_receive, replyChannel));
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                if (_reply != null)
                {
                    _reply.Dispose();
                    _reply = null;
                }
                if (_disposableOfReceiver != null)
                {
                    _disposableOfReceiver.Dispose();
                    _disposableOfReceiver = null;
                }
            }
        }
    }
}
