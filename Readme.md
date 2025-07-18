https://github.com/tosh-coding/AsyncFiberWorks

# AsyncFiberWorks #
This is a fiber-based C# threading library. The goal is to make it easy to combine fiber and asynchronous methods.

# Features #
  * Fiber with high affinity for asynchronous methods.
  * The main thread is available from Fibers.
  * Ready-to-use user thread pool.
  * .NET Standard 2.0.3 compliant simple dependencies.

# Background #

Forked from [Retlang](https://code.google.com/archive/p/retlang/). I'm refactoring it to suit my personal taste. I use it for my hobby game development.

This is still in the process of major design changes.

# Use case #

## Another Task.Run ##

"Task.Run" uses a shared thread pool in the background. If I/O wait processing is performed synchronously there, other tasks will get stuck, causing performance degradation.  This can be avoided by using a separate thread instead.

```csharp
async Task SampleAsync()
{
    // Ready-made another thread pool is available.
    await AnotherThreadPool.Instance.CreateFiber().EnqueueAsync(() =>
    {
        // It calls a blocking function, but it doesn't affect .NET ThreadPool.
        // Because it's on an another thread.
        SomeBlockingFunction();
    });

    ...
}
```

To "Fire and forget” multiple tasks in parallel, create an another thread pool.

```csharp
UserThreadPool userThreadPool = UserThreadPool.StartNew(4);
...
userThreadPool.Queue((x) => SomeReadWriteSyncAction());
...
userThreadPool.Dispose();
```

## Process in the main thread ##

Often in game libraries, there are functions that can only be called in the main thread. By treating the main thread as a task queue loop, they can be used on asynchronous contexts.

```csharp
class Program
{
    static void Main(string[] args)
    {
        // Create a task queue.
        var mainThreadLoop = new ThreadPoolAdapter();

        // Starts an asynchronous operation. Pass the task queue.
        RunAsync(mainThreadLoop);

        // Consume tasks taken from that queue, on the main thread.
        // It will not return until the task queue is stopped.
        mainThreadLoop.Run();
    }

    static async void RunAsync(ThreadPoolAdapter mainThreadLoop)
    {
        ...
        // Enqueue actions to the main thread loop.
        mainThreadLoop.Enqueue(() => someAction());
        mainThreadLoop.Enqueue(() => someAction());

        ...
        // Stop the task queue loop of the main thread .
        mainThreadLoop.Stop();
    }
```

## Guarantee execution order ##

A task queue loop running on a thread pool does not guarantee the order in which tasks are executed. Fiber can be used to guarantee the order.

```csharp
// Create a fiber that runs on the default `.NET ThreadPool`.
var fiber = new PoolFiber();

// Enqueue actions via fiber to guarantee execution order.
int counter = 0;
fiber.Enqueue(() => counter += 1);
fiber.EnqueueTask(async () =>
{
    await Task.Delay(1000);
    counter *= 100;
});
fiber.Enqueue(() => counter += 2);
fiber.Enqueue(() => Assert.AreEquals(102, counter));
```

Can wait for fiber processing completion in an asynchronous context.

```csharp
async Task SomeMethodAsync()
{
    var anotherThreadFiber = AnotherThreadPool.Instance.CreateFiber();
    anotherThreadFiber.Enqueue(() => someA());
    anotherThreadFiber.Enqueue(() => someB());
    anotherThreadFiber.Enqueue(() => someC());

    // Wait for queued actions to complete.
    await anotherThreadFiber.EnqueueAsync(() => {});
    ...
}
```

# Proper use #

## Drivers of task queue loop ##

Running on a shared thread:

- (DefaultThreadPool &) PoolFiber
- UserThreadPool & PoolFiber
- AnotherThreadPool & PoolFiber

Runs on a newly created dedicated thread:

- UserThreadPool.StartNew(1) & PoolFiber
- ConsumerThread

Runs on a dedicated specific thread:

- ThreadPoolAdapter & PoolFiber

Runs by manually pumping tasks:

- ConcurrentQueueActionQueue & ThreadPoolAdapter & PoolFiber

# API Documentation #

See API Documentation here: https://tosh-coding.github.io/AsyncFiberWorks/api/

[Unit tests](https://github.com/tosh-coding/AsyncFiberWorks/tree/main/src/AsyncFiberWorksTests) can also be used as a code sample.

## Fibers ##
Fiber is a mechanism for sequential processing.  Actions added to a fiber are executed sequentially.  `Action` and `Func<Task>` can be added.

  * _[PoolFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/PoolFiber.cs)_ - Fiber. ".NET ThreadPool" is used by default. User thread pools are also available.

## ThreadPools ##
Producer-Consumer pattern.  One or more threads become consumers and execute tasks taken from the task queue.

 * _[DefaultThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/DefaultThreadPool.cs)_ - Default implementation that uses the .NET thread pool.
 * _[UserThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/UserThreadPool.cs)_ - Another thread pool implementation, using the Thread class to create a thread pool.  If you need to use blocking functions, you should use the user thread pool. This does not disturb the .NET ThreadPool.
 * _[AnotherThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/AnotherThreadPool.cs)_ - Convenience wrapper for UserThreadPool.  There are two worker threads.
 * _[ThreadPoolAdapter](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/ThreadPoolAdapter.cs)_ - A thread pool that uses a single existing thread as a worker thread.  Convenient to combine with the main thread.

## Procedures ##
These are mechanisms for sequential processing. Call all tasks in the order in which they were registered. Wait for the calls to complete one by one before proceeding. Different fibers can be specified for each action. Can be performed repeatedly.

 * _[FiberAndTaskPairList](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/FiberAndTaskPairList.cs)_ - List of destination fiber and task pairs.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/FiberAndTaskPairListTests.cs).
 * _[FiberAndHandlerPairList{TMessage}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/FiberAndHandlerPairList.cs)_ - List of destination fiber and handler pairs. Can be used for event handling. [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/FiberAndTaskPairListTests.cs#L93).

## Channels ##
This is a mechanism for parallel processing.  A channel is a messaging mechanism that abstracts the communication destination.  Fibers act as actors. Arrival messages are processed in parallel for each fiber. 

 * _[Channel](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Channels/Channel.cs)_ - Forward published messages to all subscribers.  One-way.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ChannelTests.cs).

### Channel design concept ###
The design concept of the channel has not changed much from its source, Retlang. The following description is taken from Retlang.

> Message based concurrency in .NET
> \[...\]
> The library is intended for use in [message based concurrency](http://en.wikipedia.org/wiki/Message_passing) similar to [event based actors in Scala](http://lampwww.epfl.ch/~phaller/doc/haller07actorsunify.pdf).  The library does not provide remote messaging capabilities. It is designed specifically for high performance in-memory messaging.

(Quote from [Retlang page](https://code.google.com/archive/p/retlang/). Broken links were replaced.)

# Internal implementation note #

## How to pause context ##

| Execution context | Pause method |
|:-|:-|
| Dedicated thread | `Thread.Sleep()` |
| Fiber on shared threads | `fiber.Enqueue(Action<FiberExecutionEventArgs>) & FiberExecutionEventArgs.Pause()/Resume()` |
| Asynchronous control flow | `await Task.Deley()` |
