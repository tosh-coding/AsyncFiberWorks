using AsyncFiberWorks.Core;
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
        private readonly int _timeoutInMs;

        private IReply<T> _reply;
        private bool _disposed = false;
        private IDisposable _disposableOfReceiver;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fiber">the target executor to receive the message</param>
        /// <param name="control"></param>
        /// <param name="receive"></param>
        /// <param name="timeoutInMs">For initial snapshot</param>
        public SnapshotRequest(IExecutionContext fiber, Action<SnapshotRequestControlEvent> control, Action<T> receive, int timeoutInMs)
        {
            _fiber = fiber;
            _control = control;
            _receive = receive;
            _timeoutInMs = timeoutInMs;
        }

        internal void StartSubscribe(RequestReplyChannel<object, T> requestChannel, MessageHandlerList<T> _updatesChannel)
        {
            var reply = requestChannel.SendRequest(new object());
            if (reply == null)
            {
                throw new ArgumentException(typeof(T).Name + " synchronous request has no reply subscriber.");
            }
            _reply = reply;

            reply.SetCallbackOnReceive(_timeoutInMs, null, (_) =>
            {
                T result;
                bool successToFirstReceive = reply.TryReceive(out result);
                lock (_lock)
                {
                    _reply.Dispose();
                    _reply = null;
                }

                if (!successToFirstReceive)
                {
                    _fiber.Enqueue(() => _control(SnapshotRequestControlEvent.Timeout));
                    return;
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
            });
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
        /// A timeout occurred.
        /// </summary>
        Timeout = 1,

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
