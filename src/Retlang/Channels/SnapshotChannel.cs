using System;
using System.Threading.Tasks;
using Retlang.Fibers;

namespace Retlang.Channels
{
    ///<summary>
    /// A SnapshotChannel is a channel that allows for the transmission of an initial snapshot followed by incremental updates.
    /// The class is thread safe.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class SnapshotChannel<T> : ISnapshotChannel<T>
    {
        private readonly int _timeoutInMs;
        private readonly InternalChannel<T> _updatesChannel = new InternalChannel<T>();
        private readonly RequestReplyChannel<object, T> _requestChannel = new RequestReplyChannel<object, T>();

        ///<summary>
        ///</summary>
        ///<param name="timeoutInMs">For initial snapshot</param>
        public SnapshotChannel(int timeoutInMs)
        {
            _timeoutInMs = timeoutInMs;
        }

        ///<summary>
        /// Subscribes for an initial snapshot and then incremental update.
        ///</summary>
        ///<param name="fiber">the target executor to receive the message</param>
        ///<param name="receive"></param>
        /// <returns></returns>
        public async Task<IDisposable> PrimedSubscribe(IFiber fiber, Action<T> receive)
        {
            using (var reply = _requestChannel.SendRequest(new object()))
            {
                if (reply == null)
                {
                    throw new ArgumentException(typeof (T).Name + " synchronous request has no reply subscriber.");
                }

                await WaitOnReceive(reply, _timeoutInMs).ConfigureAwait(false);

                T result;
                if (!reply.TryReceive(out result))
                {
                    throw new ArgumentException(typeof (T).Name + " synchronous request timed out in " + _timeoutInMs);
                }

                await fiber.SwitchTo();
                try
                {
                    receive(result);
                }
                finally
                {
                    await Task.Yield();
                }

                Action<T> action = (msg) =>
                {
                    fiber.Enqueue(() => receive(msg));
                };
                return _updatesChannel.SubscribeOnProducerThreads(fiber, action);
            }
        }

        private static Task WaitOnReceive(IReply<T> reply, int timeoutInMs)
        {
            var tcs = new TaskCompletionSource<byte>();
            reply.SetCallbackOnReceive(timeoutInMs, null, (_) =>
            {
                tcs.SetResult(1);
            });
            return tcs.Task;
        }

        ///<summary>
        /// Publishes the incremental update.
        ///</summary>
        ///<param name="update"></param>
        public bool Publish(T update)
        {
            return _updatesChannel.Publish(update);
        }

        ///<summary>
        /// Ressponds to the request for an initial snapshot.
        ///</summary>
        ///<param name="fiber">the target executor to receive the message</param>
        ///<param name="reply">returns the snapshot update</param>
        public IDisposable ReplyToPrimingRequest(IFiber fiber, Func<T> reply)
        {
            return _requestChannel.Subscribe(fiber, request => request.SendReply(reply()));
        }
    }
}