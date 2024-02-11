using Retlang.Core;
using System;

namespace Retlang.Channels
{
    internal class SnapshotRequest<T> : IDisposable
    {
        private readonly object _lock = new object();
        private readonly IExecutionContext _fiber;
        private readonly Action<SnapshotRequestControlEvent> _control;

        private IReply<T> _reply;
        private bool _disposed = false;
        private IDisposable _disposableOfReceiver = null;

        public SnapshotRequest(RequestReplyChannel<object, T> requestChannel, InternalChannel<T> _updatesChannel, IExecutionContext fiber, Action<SnapshotRequestControlEvent> control, Action<T> receive, int timeoutInMs, ISubscriptionRegistry registry)
        {
            var reply = requestChannel.SendRequest(new object());
            if (reply == null)
            {
                throw new ArgumentException(typeof(T).Name + " synchronous request has no reply subscriber.");
            }
            _reply = reply;
            _fiber = fiber;
            _control = control;

            reply.SetCallbackOnReceive(timeoutInMs, null, (_) =>
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
                    fiber.Enqueue(() => control(SnapshotRequestControlEvent.Timeout));
                    return;
                }
                fiber.Enqueue(() => control(SnapshotRequestControlEvent.Connecting));

                Action<T> action = (msg) =>
                {
                    fiber.Enqueue(() => receive(msg));
                };
                action(result);
                var disposableOfReceiver = _updatesChannel.SubscribeOnProducerThreads(action);
                disposableOfReceiver = registry?.RegisterSubscriptionAndCreateDisposable(disposableOfReceiver) ?? disposableOfReceiver;
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
                        fiber.Enqueue(() => control(SnapshotRequestControlEvent.Connected));
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
