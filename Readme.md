https://github.com/tosh-coding/AsyncFiberWorks

# AsyncFiberWorks #
This is a fiber-based C# threading library. The goal is to make it easy to combine fiber and asynchronous methods.

The main thread can be handled via fiber.

```csharp
using AsyncFiberWorks.Core;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create an adapter.
            var mainThreadAdaptor = new ThreadPoolAdaptorFromQueueForThread();

            // Starts an asynchronous operation. Pass the adapter.
            RunAsync(mainThreadAdaptor);

            // Run the adapter on the main thread. It does not return until Stop is called.
            mainThreadAdaptor.Run();
        }
    }

    async void RunAsync(ThreadPoolAdaptorFromQueueForThread mainThreadAdaptor)
    {
        await Task.Yield();

        // Create a pool fiber backed the main thread.
        var mainFiber = new PoolFiber(mainThreadAdaptor, new DefaultExecutor());

        // Enqueue actions to the main thread via fiber.
        int counter = 0;
        mainFiber.Enqueue(() => counter += 1);
        mainFiber.Enqueue(() => counter += 2);

        // Switch the context to the main thread.
        await mainFiber.SwitchTo();
        counter += 3;
        counter += 4;

        // Switch the context to a .NET ThreadPool worker thread.
        await Task.Yield();

        // Stop the Run method on the main thread.
        mainThreadAdaptor.Stop();
    }
}
```

A user-created thread pool is also available. This is useful to avoid the execution of blocking functions on .NET ThreadPool.

```csharp
async Task SampleAsync()
{
    // Create a user thread pool and its fiber.
    var userThreadPool = UserThreadPool.StartNew(2);
    var fiber = new PoolFiber(userThreadPool, new DefaultExecutor());

    // Enqueue actions to a user thread pool via fiber.
    int counter = 0;
    fiber.Enqueue(() => counter += 1);
    fiber.Enqueue(() => counter += 2);

    // Switch the context to a user thread.
    await fiber.SwitchTo();

    // It calls a blocking function, but it doesn't affect .NET ThreadPool because it's on a user thread.
    var result = SomeBlockingFunction();

    // Switch the context to a .NET ThreadPool worker thread. And use the result.
    await Task.Yield();
    Console.WriteLine($"result={result}");

    userThreadPool.Dispose();
}
```

# Features #
  * The main thread is available from Fibers.
  * Fiber with high affinity for asynchronous methods.
  * Ready-to-use user thread pool.
  * .NET Standard 2.0.3 compliant simple dependencies.

# Background #

Forked from [Retlang](https://code.google.com/archive/p/retlang/). I'm refactoring it to suit my personal taste. I use it for my hobby game development.

This is still in the process of major design changes.

# API Documentation #
See https://tosh-coding.github.io/AsyncFiberWorks/api/

[Unit tests](https://github.com/tosh-coding/AsyncFiberWorks/tree/main/src/AsyncFiberWorksTests) can also be used as a code sample.

The following is a brief description of some typical functions.

## Fibers ##
Fiber is a mechanism for sequential processing.  Actions added to a fiber are executed sequentially.

  * _[PoolFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/PoolFiber.cs)_ - The most commonly used fiber.  Internally, the [.NET thread pool is used](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Core/DefaultThreadPool.cs#L21) by default, and a user thread pool is also available.
  * _[ThreadFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/ThreadFiber.cs)_ - This fiber generates and uses a dedicated thread internally.
  * _[StubFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/StubFiber.cs)_ - Fiber without consumer thread. Buffered actions are not performed automatically and must be pumped manually.  This works well with tick-based game loop implementations.

### Pause fiber ###
PoolFiber and StubFiber are supports pausing and resuming task consumption. This is useful when you want to stop consuming subsequent tasks until some asynchronous processing completes. It can be regarded as a synchronous process on that fiber.  See [unit test](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/FiberPauseResumeTests.cs#L51).

ThreadFiber does not support pause. It is specifically intended for performance-critical uses, and pausing is not suitable for that purpose.  Use PoolFiber instead.

## ThreadPools ##
 * _[DefaultThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Core/DefaultThreadPool.cs)_ - Default implementation that uses the .NET thread pool.
 * _[UserThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Core/UserThreadPool.cs)_ - Another thread pool implementation, using the Thread class to create a thread pool.  If you need to use blocking functions, you should use the user thread pool. This does not disturb the .NET ThreadPool.
 * _[ThreadPoolAdaptorFromQueueForThread](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Core/ThreadPoolAdaptorFromQueueForThread.cs)_ - A thread pool that uses a single existing thread as a worker thread.  Convenient to combine with the main thread.

## Channels ##
The channel function has not changed much from the original Retlang design concept. The following explanation is quoted from Retlang.

> Message based concurrency in .NET
> \[...\]
> The library is intended for use in [message based concurrency](http://en.wikipedia.org/wiki/Message_passing) similar to [event based actors in Scala](http://lampwww.epfl.ch/~phaller/doc/haller07actorsunify.pdf).  The library does not provide remote messaging capabilities. It is designed specifically for high performance in-memory messaging.

(Quote from [Retlang page](https://code.google.com/archive/p/retlang/). Broken links were replaced.)

### Four channels ###
There are four channel types.

 * _[Channel](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Channels/Channel.cs)_ - Forward published messages to all subscribers.  One-way.  Used for 1:1 unicasting, 1:N broadcasting and N:1 message aggregation.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/Examples/BasicExamples.cs#L20).
 * _[QueueChannel](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Channels/QueueChannel.cs)_ - Forward a published message to only one of the subscribers. One-way. Used for 1:N/N:N load balancing.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/QueueChannelTests.cs#L22).
 * _[RequestReplyChannel](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Channels/RequestReplyChannel.cs)_ - Subscribers respond to requests from publishers. Two-way.  Used for 1:1/N:1 request/reply messaging, 1:N/N:M bulk queries to multiple nodes.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/RequestReplyChannelTests.cs#L20).
 * _[SnapshotChannel](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Channels/SnapshotChannel.cs)_ - Subscribers are also notified when they start subscribing, and separately thereafter.  One-way. Used for replication with incremental update notifications.  Only one responder can be handled within a single channel.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/Examples/BasicExamples.cs#L181).
