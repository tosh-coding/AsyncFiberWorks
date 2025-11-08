using AsyncFiberWorks.PubSub;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncFiberWorksTests
{
    [TestFixture]
    public class SnapshotTest
    {
        /// <summary>
        /// Notify whenever the value held changes.
        /// </summary>
        [Test]
        public async Task Snapshot()
        {
            using (var subscriptions = new Subscriptions())
            {
                var unsubscriber = subscriptions.BeginSubscription();
                var fiberReply = new PoolFiber();
                var notificationChannel = new Channel<int>();
                Func<Channel<int>, Task<IDisposable>> requestChannel;
                Func<int, Task> setValue;

                // A value managed by the responder.
                int currentValue = 0;

                // Set up responder. 
                requestChannel = async (request) =>
                {
                    IDisposable response = default;
                    await fiberReply.EnqueueAsync(() =>
                    {
                        int value = currentValue;
                        request.Publish(value);
                        var workFiber = new PoolFiber();
                        var disposableOfReceiver = notificationChannel.Subscribe(workFiber, (msg) =>
                        {
                            request.Publish(msg);
                        });
                        response = disposableOfReceiver;
                    });
                    return response;
                };

                setValue = async (newValue) =>
                {
                    await fiberReply.EnqueueAsync(() =>
                    {
                        currentValue = newValue;
                        notificationChannel.Publish(currentValue);
                    });
                };

                // Start changing the value before subscribing.
                await setValue(1);
                await setValue(2);

                // Start requesting.
                var fiberRequest = new PoolFiber();
                var receivedValues = new List<int>();
                var receiveChannel = new Channel<int>();
                var disposable = receiveChannel.Subscribe(fiberRequest, (v) =>
                {
                    receivedValues.Add(v);
                    Console.WriteLine("Received: " + v);
                });
                unsubscriber.AppendDisposable(disposable);

                disposable = await requestChannel(receiveChannel).ConfigureAwait(false);
                unsubscriber.AppendDisposable(disposable);

                // Change the value after subscribing.
                await setValue(4);
                await setValue(8);

                // Wait publishing.
                await Task.Delay(200);

                int[] expectedReceiveValues = new int[]
                {
                    2, 4, 8,
                };

                Assert.AreEqual(expectedReceiveValues.Length, receivedValues.Count);

                for (int i = 0; i < expectedReceiveValues.Length; i++)
                {
                    Assert.AreEqual(expectedReceiveValues[i], receivedValues[i]);
                }
            }
        }
    }
}
