using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using System;

namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Subscribes for an initial snapshot and then incremental update.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SnapshotRequest<T> : IDisposable
    {
        private readonly object _lock = new object();
        private readonly IExecutionContext _fiber;
        private readonly Action<SnapshotRequestControlEvent> _control;
        private readonly Action<T> _receive;

        private IDisposable _reply;
        private bool _disposed = false;
        private IDisposable _disposableOfReceiver;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="control"></param>
        /// <param name="receive"></param>
        public SnapshotRequest(IExecutionContext fiber, Action<SnapshotRequestControlEvent> control, Action<T> receive)
        {
            _fiber = fiber;
            _control = control;
            _receive = receive;
        }

        internal void StartSubscribe(RequestReplyChannel<object, T> requestChannel, MessageHandlerList<T> _updatesChannel)
        {
            var replyChannel = new Channel<T>();
            _reply = replyChannel.Subscribe((result) => DefaultThreadPool.Instance.Queue((_) =>
            {
                lock (_lock)
                {
                    if (_reply == null)
                    {
                        return;
                    }
                    _reply.Dispose();
                    _reply = null;
                }

                _fiber.Enqueue(() => _control(SnapshotRequestControlEvent.Connecting));

                Action<T> action = (msg) =>
                {
                    _fiber.Enqueue(() => _receive(msg));
                };
                action(result);
                var disposableOfReceiver = _updatesChannel.AddHandler(action);

                lock (_lock)
                {
                    if (_disposed)
                    {
                        disposableOfReceiver.Dispose();
                        return;
                    }
                    else
                    {
                        _disposableOfReceiver = disposableOfReceiver;
                        _fiber.Enqueue(() => _control(SnapshotRequestControlEvent.Connected));
                    }
                }
            }));
            requestChannel.SendRequest(new object(), replyChannel);
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
                _fiber.Enqueue(() => _control(SnapshotRequestControlEvent.Stopped));
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
