https://github.com/tosh-coding/AsyncFiberWorks

# AsyncFiberWorks #
This is a fiber-based C# threading library. The goal is to make it easy to combine fiber and asynchronous methods.

The main thread can be handled via fiber.

```csharp
using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using AsyncFiberWorks.Threading;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create an adapter.
            var mainThreadAdaptor = new ThreadPoolAdaptor();

            // Starts an asynchronous operation. Pass the adapter.
            RunAsync(mainThreadAdaptor);

            // Run the adapter on the main thread. It does not return until Stop is called.
            mainThreadAdaptor.Run();
        }

        static async void RunAsync(ThreadPoolAdaptor mainThreadAdaptor)
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
  * Fiber with high affinity for asynchronous methods.
  * The main thread is available from Fibers.
  * Ready-to-use user thread pool.
  * .NET Standard 2.0.3 compliant simple dependencies.

# Background #

Forked from [Retlang](https://code.google.com/archive/p/retlang/). I'm refactoring it to suit my personal taste. I use it for my hobby game development.

This is still in the process of major design changes.

# API Documentation #
See https://tosh-coding.github.io/AsyncFiberWorks/api/

[Unit tests](https://github.com/tosh-coding/AsyncFiberWorks/tree/main/src/AsyncFiberWorksTests) can also be used as a code sample.

The following is a brief description of some typical functions.

## Hierarchical structure ##

### Contexts ###

1. IThreadPool - Base layer. High-speed consumer loop.
2. IExecutionContext - Second layer. Fiber. Guaranteed execution order.
3. Context triggered by message subscription.
4. Context triggered by timing subscriptions.

### Context usage ###

#### 1. SwitchTo() ####
```csharp
await threadPool.SwitchTo();
await executionContext.SwitchTo();
```

#### 2. Enqueue(Action) ####
```csharp
threadPool.Queue((_) => action());
executionContext.Enqueue(action);
```

#### 3. Subscribe(...) ####
```csharp
driver1.Subscribe(async () => {...});
driver2.Subscribe(async (ev) => {...});
```

#### 4. WaitSetting() ####
```csharp
using var reg = new AsyncRegister<Event>(driver);
while (...)
{
    var ev = await reg.WaitSetting();
    ...
}
```

### Context generation method ###

- Create `PoolFiber`. Most basic method.
- Create `ThreadPoolAdaptor`. Allows the main thread to be used like a thread pool.
- Create `UserThreadPool`. Suitable for handling blocking processes.
- Create `AsyncActionDriver` and use it with a repeating timer, which works well with tick-based game loop implementations.
- Create `AsyncActionDriver<T>` and use it for event distribution.

## Fibers ##
Fiber is a mechanism for sequential processing.  Actions added to a fiber are executed sequentially.

  * _[PoolFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/PoolFiber.cs)_ - The most commonly used fiber.  Internally, the [.NET thread pool is used](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Core/DefaultThreadPool.cs#L21) by default, and a user thread pool is also available.
  * _[ThreadFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/ThreadFiber.cs)_ - This fiber generates and uses a dedicated thread internally.
  * _[StubFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/StubFiber.cs)_ - Fiber without consumer thread. Buffered actions are not performed automatically and must be pumped manually.

### Pause fiber ###
PoolFiber and StubFiber are supports pausing and resuming task consumption. This is useful when you want to stop consuming subsequent tasks until some asynchronous processing completes. It can be regarded as a synchronous process on that fiber.  See [unit test](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/FiberPauseResumeTests.cs#L51).

ThreadFiber does not support pause. It is specifically intended for performance-critical uses, and pausing is not suitable for that purpose.  Use PoolFiber instead.

## ThreadPools ##
 * _[DefaultThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/DefaultThreadPool.cs)_ - Default implementation that uses the .NET thread pool.
 * _[UserThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/UserThreadPool.cs)_ - Another thread pool implementation, using the Thread class to create a thread pool.  If you need to use blocking functions, you should use the user thread pool. This does not disturb the .NET ThreadPool.
 * _[ThreadPoolAdaptor](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/ThreadPoolAdaptor.cs)_ - A thread pool that uses a single existing thread as a worker thread.  Convenient to combine with the main thread.

## Drivers ##
Drivers provide the timing of execution. It provides methods for invoking and subscribing to actions. Execution is done serially.

 * _[ActionDriver](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/ActionDriver.cs)_ - Execute registered actions in bulk. [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ActionDriverTests.cs#L12).
 * _[AsyncActionDriver](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/AsyncActionDriver.cs)_ - Executes registered asynchronous tasks in bulk. [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ActionDriverTests.cs#L38).
 * _[AsyncActionDriver{T}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/AsyncActionDriverOfT.cs)_ - Executes registered asynchronous tasks in bulk.  Arguments can be specified.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ActionDriverTests.cs#L66).
 * _[AsyncActionDriver{TArg, TRet}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/AsyncActionDriverOfTArgTRet.cs)_ - Executes registered asynchronous tasks in bulk.  Arguments and return values can be specified.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/AsyncActionDriverOfTArgTRetTests.cs).

## Channels ##
This is a mechanism for parallel processing. If you do not need that much performance, `AsyncActionDriver{T}` is recommended because it is easier to handle.

A channel is a messaging mechanism that abstracts the communication destination.  Fibers act as actors. Arrival messages are processed in parallel for each fiber. 

 * _[Channel](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Channels/Channel.cs)_ - Forward published messages to all subscribers.  One-way.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ChannelTests.cs#L18).

### Channel design concept ###
The design concept of the channel has not changed much from its source, Retlang. The following description is taken from Retlang.

> Message based concurrency in .NET
> \[...\]
> The library is intended for use in [message based concurrency](http://en.wikipedia.org/wiki/Message_passing) similar to [event based actors in Scala](http://lampwww.epfl.ch/~phaller/doc/haller07actorsunify.pdf).  The library does not provide remote messaging capabilities. It is designed specifically for high performance in-memory messaging.

(Quote from [Retlang page](https://code.google.com/archive/p/retlang/). Broken links were replaced.)
