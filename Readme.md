# RetlangFiberSwitcher
RetlangFiberSwitcher is a fiber-based C# threading library. Forked from [Retlang](https://code.google.com/archive/p/retlang/). A new feature is the ability to switch the fiber in use within an async method.

```csharp
async Task TestAsync()
{
    // Create fibers.
    var threadFiber = ThreadFiberSlim.StartNew();
    var poolFiber = PoolFiberSlim.StartNew();

    // Create a user thread pool and its fiber.
    var userThreadPool = UserThreadPool.StartNew(2);
    var userPoolFiber = PoolFiberSlim.StartNew(userThreadPool, new DefaultExecutor());

    // Switche to operate on the fibers.
    await threadFiber.SwitchTo();
    // Do something.
    await poolFiber.SwitchTo();
    // Do something.
    await userPoolFiber.SwitchTo();
    // Do something.

    // It can also be switched to operate on the thread pools.
    await userThreadPool.SwitchTo();
    // Do something.
    await DefaultThreadPool.Instance.SwitchTo();
    // Do something.

    ...
}
```

[Test code for SwitchTo](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/RetlangTests/SwitchToTests.cs)

# Introduction of Retlang (Quote) #
(Quote from [Retlang page](https://code.google.com/archive/p/retlang/). Broken links were replaced.)

Message based concurrency in .NET

Retlang is a high performance C# threading library (see [Jetlang](http://code.google.com/p/jetlang/) for a version in Java).  The library is intended for use in [message based concurrency](http://en.wikipedia.org/wiki/Message_passing) similar to [event based actors in Scala](http://lampwww.epfl.ch/~phaller/doc/haller07actorsunify.pdf).  The library does not provide remote messaging capabilities. It is designed specifically for high performance in-memory messaging.

# Features of Retlang (Quote) #
(Quote from [Retlang page](https://code.google.com/archive/p/retlang/). Broken links were replaced.)

All messages to a particular [IFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiber.cs) are delivered sequentially. Components can easily keep state without synchronizing data access or worrying about thread races.
  * Single [IFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiber.cs) interface that can be backed by a [dedicated thread](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/ThreadFiber.cs), a [thread pool](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/PoolFiber.cs), or a [WinForms](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/WpfExample/FormFiber.cs)/[WPF](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/WpfExample/DispatcherFiber.cs) message pump .
  * Supports single or multiple subscribers for messages.
  * Subscriptions for single events or event batching.
  * [Single](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/SchedulerForBackwardCompatibilityExtensions.cs#L18) or [recurring](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/SchedulerForBackwardCompatibilityExtensions.cs#L31) event scheduling.
  * High performance design optimized for low latency and high scalability.
  * Publishing is thread safe, allowing easy integration with other threading models.
  * Low Lock Contention - Minimizing lock contention is critical for performance. Other concurrency solutions are limited by a single lock typically on a central thread pool or message queue. Retlang is optimized for low lock contention. Without a central bottleneck, performance easily scales to the needs of the application.
  * [Synchronous](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/RequestReplyChannel.cs)/[Asynchronous](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/RetlangTests/Channels/ChannelTests.cs#L171) request-reply support.
  * Single assembly with no dependencies except the CLR (4.0+).

Retlang relies upon four abstractions: [IFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiber.cs),
[IQueue](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/IQueue.cs),  [IExecutor](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/IExecutor.cs), and [IChannel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/IChannel.cs).  An [IFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiber.cs) is an abstraction for the [context of execution](http://en.wikipedia.org/wiki/Context_switch) (in most cases a thread).  An [IQueue](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/IQueue.cs) is an abstraction for the data structure that holds the actions until the IFiber is ready for more actions.  The default implementation, [DefaultQueue](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/DefaultQueue.cs), is an unbounded storage that uses standard locking to notify when it has actions to execute.  An [IExecutor](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/IExecutor.cs) performs the actual execution.  It is useful as an injection point to achieve fault tolerance, performance profiling, etc.  The default implementation, [DefaultExecutor](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/DefaultExecutor.cs), simply executes actions.  An [IChannel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/IChannel.cs) is an abstraction for the conduit through which two or more [IFibers](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiber.cs) communicate (pass messages).

# New features of RetlangFiberSwitcher #
 * Awaitable fiber.SwitchTo() method.  It switches the operation context to the specified fiber.
 * Added a thread pool implementation: [UserThreadPool](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/UserThreadPool.cs). It can be used when many blocking functions must be used. Blocking within the [.NET ThreadPool](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/DefaultThreadPool.cs#L21) should be avoided.
 * Added [simple fibers](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiberSlim.cs) for non-Channels use.
 * TargetFramework was Changed to .NET Standard 2.0.

# Quick Start #

## FiberSlims ##
[IFiberSlims](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiberSlim.cs) is a mechanism for sequential processing.  Actions added to a fiber are executed sequentially.  Three implementations of IFiberSlim are included in RetlangFiberSwitcher.

  * _[ThreadFiberSlim](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/ThreadFiberSlim.cs)_ - an IFiberSlim backed by a dedicated thread.  Internally, it works using [IQueue](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/IQueue.cs).
  * _[PoolFiberSlim](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/PoolFiberSlim.cs)_ - an IFiberSlim backed by shared threads.  Internally, it works using [IThreadPool](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Core/IThreadPool.cs).  The .NET thread pool is used by default, and the user thread pool is also available.  See [unit test](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/RetlangTests/ThreadPoolTests.cs#L156).
  * _[StubFiberSlim](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/StubFiberSlim.cs)_ - an IFiberSlim without a specific thread.  Actions are buffered in the queue.  Useful for consumption in periodic processing.

## Fibers for backward compatibility ##
[IFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiber.cs) is iFiberSlim with subscription and scheduling registration functions.  Three implementations of IFiber are included in RetlangFiberSwitcher. 

  * _[ThreadFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/ThreadFiber.cs)_ - an IFiber based [ThreadFiberSlim](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/ThreadFiberSlim.cs).
  * _[PoolFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/PoolFiber.cs)_ - an IFiber based [PoolFiberSlim](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/PoolFiberSlim.cs).
  * _[StubFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/StubFiber.cs)_ - an IFiber based [StubFiberSlim](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/StubFiberSlim.cs).

Some Retlang's fibers have been moved to the WpfExample. Copy it from there if you need it.
  * (Quote from [Retlang page](https://code.google.com/archive/p/retlang/)) _[FormFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/WpfExample/FormFiber.cs)/[DispatchFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/WpfExample/DispatcherFiber.cs)_ - an [IFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Fibers/IFiber.cs) backed by a [WinForms](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/WpfExample/FormFiber.cs)/[WPF](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/WpfExample/DispatcherFiber.cs) message pump.  The [FormFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/WpfExample/FormFiber.cs)/[DispatchFiber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/WpfExample/DispatcherFiber.cs) entirely removes the need to call Invoke or BeginInvoke to communicate with a window from a different thread.

## Channels ##
There are four channel types.

 * _[Channel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/Channel.cs)_ - Forward published messages to all subscribers. Used for broadcasting.
 * _[QueueChannel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/QueueChannel.cs)_ - Forward a published message to only one of the subscribers. Used for load balancing.
 * _[RequestReplyChannel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/RequestReplyChannel.cs)_ - Subscribers respond to requests from publishers. Used for request/reply messaging.
 * _[SnapshotChannel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/SnapshotChannel.cs)_ - Subscribers are also notified when they start subscribing, and separately thereafter. Used for replication, incremental update notifications and change notifications.

### Several ways to receive messages from the Channel ###
[Channel](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/Channel.cs) class provides several ways to receive messages. Subscribers can choose from them when they start subscribing.

  * _[Subscribe](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/ISubscriber.cs#L19)_ - Messages reach a subscriber each time they are published.  Internally, [ChannelSubscription](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/ChannelSubscription.cs) is used.
  * _[SubscribeToBatch](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/ISubscriber.cs#L29)_ - Published messages are first buffered. And over time it will reach a subscriber in bulk.  Internally, [BatchSubscriber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/BatchSubscriber.cs) is used.
  * _[SubscribeToKeyedBatch](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/ISubscriber.cs#L40)_ - Published messages are buffered first, but may overwrite the buffer. If the key extracted from the new message matches the old buffered message, the old one is deleted. And over time, it will reach a subscriber in bulk. Internally, [KeyedBatchSubscriber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/KeyedBatchSubscriber.cs) is used.
  * _[SubscribeToLast](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/ISubscriber.cs#L50)_ - Published messages are buffered first, but always overwrite the buffer. And over time, only the latest messages reach subscribers. Internally, [LastSubscriber](https://github.com/github-tosh/RetlangFiberSwitcher/blob/master/src/Retlang/Channels/LastSubscriber.cs) is used.

[Unit tests](https://github.com/github-tosh/RetlangFiberSwitcher/tree/master/src/RetlangTests) can also be used as a code sample.
