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
            var mainThread = new ThreadPoolAdaptor();

            // Starts an asynchronous operation. Pass the adapter.
            RunAsync(mainThread);

            // Run the adapter on the main thread. It does not return until Stop is called.
            mainThread.Run();
        }

        static async void RunAsync(ThreadPoolAdaptor mainThread)
        {
            // Switch the context to a .NET ThreadPool worker thread.
            await DefaultThreadPool.Instance.SwitchTo();

            // Create a pool fiber backed the main thread.
            var fiber = new PoolFiber(mainThread);

            // Enqueue actions to the main thread via fiber.
            int counter = 0;
            fiber.Enqueue(() => counter += 1);
            fiber.Enqueue(() => counter += 2);

            // Wait for queued actions to complete.
            // Then switch the context to the fiber.
            await fiber.SwitchTo();
            counter += 3;
            counter += 4;

            // When an async lambda expression is enqueued,
            // the fiber waits until it is completed.
            fiber.EnqueueTask(async () =>
            {
                await Task.Delay(1000);

                // Switch the context to the main thread.
                await mainThread.SwitchTo();

                zounter += 5;
            });

            await fiber.SwitchTo();

            // Stop the Run method on the main thread.
            mainThread.Stop();
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
    var fiber = new PoolFiber(userThreadPool);

    // Enqueue actions to a user thread pool via fiber.
    int counter = 0;
    fiber.Enqueue(() => counter += 1);
    fiber.Enqueue(() => counter += 2);

    // Wait for queued actions to complete.
    // Then switch the context to the fiber on a user thread.
    await fiber.SwitchTo();

    // It calls a blocking function, but it doesn't affect .NET ThreadPool because it's on a user thread.
    var result = SomeBlockingFunction();

    // Switch the context to a .NET ThreadPool worker thread.
    await DefaultThreadPool.Instance.SwitchTo();

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
3. Context triggered by event/message subscription.
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
driver1.SubscribeAndReceiveAsTask(async (ev) => {...});
driver2.SubscribeAndReceiveAsTask(async () => {...});
```

#### 4. Subscribe(...) via AsyncRegister ####
```csharp
using var reg = new AsyncRegister<SomeEvent>(driver3);
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
- Create `ActionDriver` and use it with a repeating timer, which works well with tick-based game loop implementations.
- Create `AsyncMessageDriver<T>` and use it for event distribution.

### How to pause contexts ###

| Execution context type | Method |
|:-|:-|
| Threads | `Thread.Sleep()` |
| Fiber on dedicated thread | `Thread.Sleep()` |
| Fiber on shared threads | `fiber.Enqueue(Action<FiberExecutionEventArgs>) & FiberExecutionEventArgs.Pause()/Resume()` |
| Asynchronous control flow | `await Task.Deley()` |

## Fibers ##
Fiber is a mechanism for sequential processing.  Actions added to a fiber are executed sequentially.

  * _[PoolFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/PoolFiber.cs)_ - The most commonly used fiber.  Internally, the [.NET thread pool is used](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Core/DefaultThreadPool.cs#L21) by default, and a user thread pool is also available.
  * _[ThreadFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/ThreadFiber.cs)_ - This fiber generates and uses a dedicated thread internally.
  * _[StubFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/StubFiber.cs)_ - Fiber without consumer thread. Buffered actions are not performed automatically and must be pumped manually.
  * _[AsyncFiber]()_ - Fiber implementation built with asynchronous control flow. It's operating thread is unstable.

## ThreadPools ##
 * _[DefaultThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/DefaultThreadPool.cs)_ - Default implementation that uses the .NET thread pool.
 * _[UserThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/UserThreadPool.cs)_ - Another thread pool implementation, using the Thread class to create a thread pool.  If you need to use blocking functions, you should use the user thread pool. This does not disturb the .NET ThreadPool.
 * _[ThreadPoolAdaptor](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/ThreadPoolAdaptor.cs)_ - A thread pool that uses a single existing thread as a worker thread.  Convenient to combine with the main thread.

## Drivers ##
Drivers call their own Subscriber handlers. There are two types: timing notification and message delivery.  They are processed in series.

 * _[ActionDriver](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/ActionDriver.cs)_ - Calls the subscriber's handler. It runs on one fiber. [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ActionDriverTests.cs).
 * _[AsyncMessageDriver{TMessage}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/MessageDrivers/AsyncMessageDriver.cs)_ - It distributes messages to subscribers.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ActionDriverTests.cs#L68).

## Channels ##
This is a mechanism for parallel processing. If you do not need that much performance, `AsyncMessageDriver{T}` is recommended. It is easy to handle because it is serial.

A channel is a messaging mechanism that abstracts the communication destination.  Fibers act as actors. Arrival messages are processed in parallel for each fiber. 

 * _[Channel](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Channels/Channel.cs)_ - Forward published messages to all subscribers.  One-way.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ChannelTests.cs).

### Channel design concept ###
The design concept of the channel has not changed much from its source, Retlang. The following description is taken from Retlang.

> Message based concurrency in .NET
> \[...\]
> The library is intended for use in [message based concurrency](http://en.wikipedia.org/wiki/Message_passing) similar to [event based actors in Scala](http://lampwww.epfl.ch/~phaller/doc/haller07actorsunify.pdf).  The library does not provide remote messaging capabilities. It is designed specifically for high performance in-memory messaging.

(Quote from [Retlang page](https://code.google.com/archive/p/retlang/). Broken links were replaced.)
