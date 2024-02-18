https://github.com/github-tosh/RetlangFiberSwitcher

# AsyncFiberWorks #
This is a fiber-based C# threading library. The goal is to make it easy to combine fiber and asynchronous methods.

In addition to enqueueing actions to a fiber, the execution context of the asynchronous method can be changed to within a fiber.

```csharp
async Task SampleAsync()
{
    // Create a fiber that uses the .NET ThreadPool.
    var fiber = new PoolFiber();

    // Enqueue actions to the fiber.
    int counter = 0;
    fiber.Enqueue(() => counter += 1);
    fiber.Enqueue(() => counter += 2);

    // Switch context to the fiber and perform the action.
    await fiber.SwitchTo();
    counter += 3;

    // Switch context to a .NET ThreadPool worker thread.
    await Task.Yield();
}
```

The default is to use .NET ThreadPool, but user-created thread pools are also available.

```csharp
async Task Sample2Async()
{
    // Create a user thread pool and its fiber.
    var userThreadPool = UserThreadPool.StartNew(2);
    var fiber = new PoolFiber(userThreadPool, new DefaultExecutor());

    // Enqueue actions to the fiber. They are executed on worker threads in the user thread pool.
    fiber.Enqueue(() => counter += 1);
    fiber.Enqueue(() => counter += 2);
    ...

    userThreadPool.Dispose();
}
```

# Features #
  * Fiber with high affinity for asynchronous methods.
  * Ready-to-use user thread pool.
  * .NET Standard 2.0.3 compliant simple dependencies.

# Background #

Forked from [Retlang](https://code.google.com/archive/p/retlang/).  I'm refactoring it to suit my personal taste. This is still in the process of major design changes.

The project name was RetlangFiberSwitcher, but refactoring changed the design from Retlang and made it no longer backward compatible. Therefore, the name was changed to AsyncFiberWorks. URLs and namespaces are still out of date.

# API Documentation #
See https://github-tosh.github.io/RetlangFiberSwitcher/api/

[Unit tests](https://github.com/github-tosh/RetlangFiberSwitcher/tree/master/src/RetlangTests) can also be used as a code sample.

The following is a brief description of some typical functions.

## Fibers ##
Fiber is a mechanism for sequential processing.  Actions added to a fiber are executed sequentially.

  * _[PoolFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/PoolFiber.cs)_ - The most commonly used fiber.  Internally, the [.NET thread pool is used](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/DefaultThreadPool.cs#L21) by default, and a user thread pool is also available.
  * _[ThreadFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/ThreadFiber.cs)_ - This fiber generates and uses a dedicated thread internally.
  * _[StubFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/StubFiber.cs)_ - Fiber without consumer thread. Buffered actions are not performed automatically and must be pumped manually.  This works well with tick-based game loop implementations.

### Pause fiber ###
PoolFiber and StubFiber are supports pausing and resuming task consumption. This is useful when you want to stop consuming subsequent tasks until some asynchronous processing completes. It can be regarded as a synchronous process on that fiber.  See [unit test](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/RetlangTests/FiberPauseResumeTests.cs#L51).

ThreadFiber does not support pause. It is specifically intended for performance-critical uses, and pausing is not suitable for that purpose.  Use PoolFiber instead.

## ThreadPools ##
 * _[DefaultThreadPool](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/DefaultThreadPool.cs)_ - Default implementation that uses the .NET thread pool.
 * _[UserThreadPool](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/UserThreadPool.cs)_ - Another thread pool implementation, using the Thread class to create a thread pool.  If you need to use blocking functions, you should use the user thread pool. This does not disturb the .NET ThreadPool.
 * _[ThreadPoolAdaptorFromQueueForThread](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/UserThreadPool.cs)_ - A thread pool that uses a single existing thread as a worker thread.  Convenient to combine with the main thread.

## Channels ##
The channel function has not changed much from the original Retlang design concept. The following explanation is quoted from Retlang.

> Message based concurrency in .NET
> \[...\]
> The library is intended for use in [message based concurrency](http://en.wikipedia.org/wiki/Message_passing) similar to [event based actors in Scala](http://lampwww.epfl.ch/~phaller/doc/haller07actorsunify.pdf).  The library does not provide remote messaging capabilities. It is designed specifically for high performance in-memory messaging.

(Quote from [Retlang page](https://code.google.com/archive/p/retlang/). Broken links were replaced.)

### Four channels ###
There are four channel types.

 * _[Channel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/Channel.cs)_ - Forward published messages to all subscribers.  One-way.  Used for 1:1 unicasting, 1:N broadcasting and N:1 message aggregation.  [Example](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/RetlangTests/Examples/BasicExamples.cs#L20).
 * _[QueueChannel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/QueueChannel.cs)_ - Forward a published message to only one of the subscribers. One-way. Used for 1:N/N:N load balancing.  [Example](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/RetlangTests/QueueChannelTests.cs#L22).
 * _[RequestReplyChannel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/RequestReplyChannel.cs)_ - Subscribers respond to requests from publishers. Two-way.  Used for 1:1/N:1 request/reply messaging, 1:N/N:M bulk queries to multiple nodes.  [Example](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/RetlangTests/RequestReplyChannelTests.cs#L20).
 * _[SnapshotChannel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/SnapshotChannel.cs)_ - Subscribers are also notified when they start subscribing, and separately thereafter.  One-way. Used for replication with incremental update notifications.  Only one responder can be handled within a single channel.  [Example](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/RetlangTests/Examples/BasicExamples.cs#L162).
