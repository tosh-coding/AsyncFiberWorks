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
        private readonly Channel<SnapshotRequestControlEvent> _control;
        private readonly Channel<T> _receive;

        private IDisposable _reply;
        private bool _disposed = false;
        private IDisposable _disposableOfReceiver;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="receive"></param>
        public SnapshotRequest(Channel<SnapshotRequestControlEvent> control, Channel<T> receive)
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
                    _control.Publish(SnapshotRequestControlEvent.Connecting);
                    _disposableOfReceiver = disposableOfReceiver;
                    _control.Publish(SnapshotRequestControlEvent.Connected);
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
                _control.Publish(SnapshotRequestControlEvent.Stopped);
            }
        }
    }

    /// <summary>
    /// A state change event for a request to a snapshot channel.
    /// </summary>
    public enum SnapshotRequestControlEvent : byte
    {
        /// <summary>
        /// Initial value. Not used.
        /// </summary>
        None = 0,

        /// <summary>
        /// The state changed during a connection attempt.
        /// </summary>
        Connecting = 2,

        /// <summary>
        /// The state has changed to a stopped state.
        /// </summary>
        Stopped = 3,

        /// <summary>
        /// The connection was successful.
        /// </summary>
        Connected = 4,
    }
}
