using AsyncFiberWorks.Channels;
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;
using NUnit.Framework;
using System;
using System.Threading;

namespace AsyncFiberWorksTests.Examples
{
    
    [TestFixture]
    [Category("Demo")]
    [Ignore("Demo")]
    public class FibonacciDemonstration
    {
        // Simple immutable class that serves as a message
        // to be passed between services.
        class IntPair
        {
            private readonly int _first;
            private readonly int _second;

            public IntPair(int first, int second)
            {
                _first = first;
                _second = second;
            }

            public int First
            {
                get { return _first; }
            }

            public int Second
            {
                get { return _second; }
            }
        }

        // This class calculates the next value in a Fibonacci sequence.
        // It listens for the previous pair on one topic, and then publishes
        // a new pair with the latest value onto the reply topic.
        // When a specified limit is reached, it stops processing.
        class FibonacciCalculator
        {
            private readonly Action _onCompleted;
            private readonly string _name;
            private readonly ISubscriber<IntPair> _inboundChannel;
            private readonly IChannel<IntPair> _outboundChannel;
            private readonly int _limit;

            public FibonacciCalculator(ConsumerThread fiber, string name, 
                ISubscriber<IntPair> inboundChannel, 
                IChannel<IntPair> outboundChannel,
                int limit,
                Action onCompleted,
                Subscriptions subscriptions)
            {
                _onCompleted = onCompleted;
                _name = name;
                _inboundChannel = inboundChannel;
                _outboundChannel = outboundChannel;
                var subscriptionFiber = subscriptions.BeginSubscription();
                var subscriptionChannel = _inboundChannel.Subscribe(fiber, CalculateNext);
                subscriptionFiber.AppendDisposable(subscriptionChannel);
                _limit = limit;
            }

            public void Begin(IntPair pair)
            {
                Console.WriteLine(_name + " " + pair.Second);
                _outboundChannel.Publish(pair);
            }

            private void CalculateNext(IntPair receivedPair)
            {
                var next = receivedPair.First + receivedPair.Second;

                var pairToPublish = new IntPair(receivedPair.Second, next);
                _outboundChannel.Publish(pairToPublish);
                
                if (next > _limit)
                {
                    Console.WriteLine("Stopping " + _name);
                    _onCompleted();
                    
                    return;
                }
                Console.WriteLine(_name + " " + next);
            }
        }

        [Test]
        public void DoDemonstration()
        {
            // Two instances of the calculator are created.  One is named "Odd" 
            // (it calculates the 1st, 3rd, 5th... values in the sequence) the
            // other is named "Even".  They message each other back and forth
            // with the latest two values and successively build the sequence.
            var limit = 1000;

            // Two channels for communication.  Naming convention is inbound.
            var oddChannel = new Channel<IntPair>();
            var evenChannel = new Channel<IntPair>();
            var sem = new SemaphoreSlim(0);
            Action onCompleted = () =>
            {
                sem.Release(1);
            };

            using (ConsumerThread oddFiber = ConsumerThread.StartNew(), evenFiber = ConsumerThread.StartNew())
            using (Subscriptions oddSubscriptions = new Subscriptions(), evenSubscriptions = new Subscriptions())
            {
                var oddCalculator = new FibonacciCalculator(oddFiber, "Odd", oddChannel, evenChannel, limit, onCompleted, oddSubscriptions);

                new FibonacciCalculator(evenFiber, "Even", evenChannel, oddChannel, limit, onCompleted, evenSubscriptions);

                oddCalculator.Begin(new IntPair(0, 1));

                sem.Wait();
                sem.Wait();
            }
        }
    }
}