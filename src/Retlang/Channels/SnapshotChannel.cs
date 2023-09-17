using System;
using System.Threading.Tasks;
using Retlang.Core;
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
        private readonly InternalChannel<T> _updatesChannel = new InternalChannel<T>();
        private readonly RequestReplyChannel<object, T> _requestChannel = new RequestReplyChannel<object, T>();

        ///<summary>
        /// Subscribes for an initial snapshot and then incremental update.
        ///</summary>
        ///<param name="fiber">the target executor to receive the message</param>
        ///<param name="receive"></param>
        ///<param name="timeoutInMs">For initial snapshot</param>
        /// <returns></returns>
        public async Task<IDisposable> PrimedSubscribe(IFiber fiber, Action<T> receive, int timeoutInMs)
        {
            using (var reply = _requestChannel.SendRequest(new object()))
            {
                if (reply == null)
                {
                    throw new ArgumentException(typeof (T).Name + " synchronous request has no reply subscriber.");
                }

                await WaitOnReceive(reply, timeoutInMs).ConfigureAwait(false);

                T result;
                if (!reply.TryReceive(out result))
                {
                    throw new ArgumentException(typeof (T).Name + " synchronous request timed out in " + timeoutInMs);
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

        ///<summary>
        /// Ressponds to the request for an initial snapshot.
        ///</summary>
        ///<param name="executionContext">the target executor to receive the message</param>
        ///<param name="reply">returns the snapshot update</param>
        public void PersistentReplyToPrimingRequest(IExecutionContext executionContext, Func<T> reply)
        {
            _requestChannel.PersistentSubscribe(executionContext, request => request.SendReply(reply()));
        }


        ///<summary>
        /// Number of subscribers
        ///</summary>
        public int NumSubscribers { get { return _requestChannel.NumSubscribers; } }

        ///<summary>
        /// Number of persistent subscribers.
        ///</summary>
        public int NumPersistentSubscribers { get { return _requestChannel.NumPersistentSubscribers; } }
    }
}