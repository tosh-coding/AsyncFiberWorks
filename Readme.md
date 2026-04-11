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
For that purpose, you can use "AnotherThreadPool.Instance.Queue".

```csharp
async Task SampleAsync()
{
    // Ready-made another thread pool is available.
    // You can wait for completion in an asynchronous context.
    await AnotherThreadPool.Instance.QueueAsync((_) =>
    {
        // It calls a blocking function, but it doesn't affect .NET ThreadPool.
        // Because it's on an another thread.
        SomeBlockingFunction();
    });

    ...
}
```

You can also create thread pools other than AnotherThreadPool.

```csharp
// Create a thread pool with 4 worker threads.
UserThreadPool userThreadPool = UserThreadPool.StartNew(4);
...
userThreadPool.Queue((_) => SomeReadWriteSyncAction());
...
userThreadPool.Dispose();
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
fiber.Enqueue(() => Assert.AreEqual(102, counter));
```

## Process in the main thread ##

Game libraries often contain functions that can only be called from the main thread. This library treats the main thread as a task queue consumption loop, making it easier to handle within asynchronous control flows.

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
        mainThreadLoop.Queue((_) => someAction());
        mainThreadLoop.Queue((_) => someAction());

        ...
        // Fiber can also run on the main thread.
        // Enqueue actions to the main thread loop via fiber.
        var fiberA = new PoolFiber(mainThreadLoop);
        fiberA.Enqueue(() => someAction());
        fiberA.Enqueue(() => someAction());

        var fiberB = new PoolFiber(mainThreadLoop);
        fiberB.Enqueue(() => someAction());
        fiberB.Enqueue(() => someAction());

        ...
        // Stop the task queue loop of the main thread .
        mainThreadLoop.Stop();
    }
```

If you want to manually pump tasks, use a combination of ConcurrentQueueActionQueue and ThreadPoolAdapter.

```csharp
ConcurrentQueueActionQueue _queue;
IFiber _fiber;

void Start()
{
    _queue = new ConcurrentQueueActionQueue();
    _fiber = new PoolFiber(new ThreadPoolAdapter(_queue));
    SomeTaskAsync(...);
}

void Update()
{
    _queue.ExecuteOnlyPendingNow();
}

async void SomeTaskAsync(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        await Task.Delay(500, token);
        ...
        _fiber.Enqueue(() => { ... });
        ...
    }
}
```

## Coroutines ##

`await` is useful when implementing coroutines with complex state transitions. While `await` typically utilizes a worker thread of the `.NET` ThreadPool class, you can also perform operations on a specific fiber using `IFiber.SwitchTo()`, `IFiber.EnqueueAsync()`, and `IFiber.EnqueueTaskAsync()`. These methods can also be used to run multiple coroutines on the main thread.

This library enables smooth transitions between threads, fibers, and asynchronous control flows.

## Event based actors ##

An event-based actor model is available, inherited from Retlang. The following description is taken from Retlang.

> Message based concurrency in .NET
> \[...\]
> The library is intended for use in [message based concurrency](http://en.wikipedia.org/wiki/Message_passing) similar to [event based actors in Scala](http://lampwww.epfl.ch/~phaller/doc/haller07actorsunify.pdf).  The library does not provide remote messaging capabilities. It is designed specifically for high performance in-memory messaging.

(Quote from [Retlang page](https://code.google.com/archive/p/retlang/). Broken links were replaced.)

Each Fiber can be treated as a single Actor. Coroutines using `await` can also be treated as Actors, but their performance will likely be lower than that of event-based Fiber actors.

When treating Fibers as actors, directly calling fiber.Enqueue for event propagation is fast, but it introduces complex dependencies. Using channels with DI or locators is slightly slower, but allows for looser coupling. Furthermore, it simplifies the design by only handling the propagation of events that have already occurred on the channel. This is particularly suitable for fire-and-forget or one-way processing, such as data distribution (fan-out).

Now that C# has `await`, I think there are fewer situations where using an event-based actor model is more efficient. Personally, I recommend using it only when the following conditions are met.

- Do not read or write shared data between actors.
- Events should be sent via a channel. Channels are obtained through DI or a locator. Do not use fiber.Enqueue directly.
- The messages we deal with must only be about "what happened."  They must represent facts that have already occurred, not requests or commands.
- Avoid using transactions that span multiple actors. If such behavior is necessary, the event-based actor model is not suitable.

# API Documentation #

See API Documentation here: https://tosh-coding.github.io/AsyncFiberWorks/api/

[Unit tests](https://github.com/tosh-coding/AsyncFiberWorks/tree/main/src/AsyncFiberWorksTests) can also be used as a code sample.

## Fibers ##
Fiber is a mechanism for sequential processing. It is also called a task queue. Actions added to a fiber are executed sequentially.  `Action` and `Func<Task>` can be added. Multiple fibers can run on one or more threads.

  * _[PoolFiber](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Fibers/PoolFiber.cs)_ - Fiber. ".NET ThreadPool" is used by default. User thread pools are also available.

## ThreadPools ##
ThreadPool is a mechanism where multiple worker threads process a given task. Producer-Consumer pattern.  One or more threads become consumers and execute tasks taken from the task queue.

 * _[DefaultThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/DefaultThreadPool.cs)_ - Default implementation that uses the .NET thread pool.
 * _[UserThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/UserThreadPool.cs)_ - Another thread pool implementation, using the Thread class to create a thread pool.  If you need to use blocking functions, you should use the user thread pool. This does not disturb the .NET ThreadPool.
 * _[AnotherThreadPool](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/AnotherThreadPool.cs)_ - Convenience wrapper for UserThreadPool.  There are two worker threads.
 * _[ThreadPoolAdapter](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Threading/ThreadPoolAdapter.cs)_ - A thread pool that uses a single existing thread as a worker thread.  Convenient to combine with the main thread.

## PubSub ##
These are mechanisms for loosely coupling messaging within a process.

A design that specifies a destination Fiber and sends messages directly results in tight coupling. This is not a problem if the design is small or speed is the top priority. However, as the design scale increases, the disadvantages often outweigh the benefits. In such cases, this mechanism can be used.

By replacing existing messaging code with code that uses these Pub/Sub interfaces, you can write sending code without specifying a destination. While the dependency on the Pub/Sub interface remains, messaging is one level more loosely coupled than before.

 * _[IPublisher{T}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/PubSub/IPublisher.cs)_ - This is a message sending interface. It can be delivered to subscribers via the same type ISubscriber.
 * _[ISubscriber{T}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/PubSub/ISubscriber.cs)_ - This is a message subscription interface. When subscribing, you can receive messages from the same type of IPublisher.
 * _[IPublisher{TKey,TMessage}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/PubSub/IPublisher_TKey_TMessage.cs)_ - This is a message sending interface. It can be delivered to subscribers via the same type ISubscriber.  The difference with IPublisher{T} is that it allows you to specify different channel instances for the same message type using different keys.
 * _[ISubscriber{TKey, TMessage}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/PubSub/ISubscriber_TKey_TMessage.cs)_ - This is a message subscription interface. When subscribing, you can receive messages from the same type of IPublisher.  The difference with ISubscriber{T} is that it allows you to specify different channel instances for the same message type using different keys.
 * _[Channel](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/PubSub/Channel.cs)_ - This is the implementation class for IPublisher and ISubscriber. Forward published messages to all subscribers.  [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/ChannelTests.cs).

## Procedures ##
These are mechanisms for sequential processing when using multiple fibers. Call all tasks in the order in which they were registered. Wait for the calls to complete one by one before proceeding. Different fibers can be specified for each action. Can be performed repeatedly.

 * _[FiberAndTaskPairList](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/FiberAndTaskPairList.cs)_ - List of fiber and task pairs. Tasks using different fibers can be processed sequentially. Can be used repeatedly. [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/FiberAndTaskPairListTests.cs).
 * _[FiberAndHandlerPairList{TMessage}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/FiberAndHandlerPairList.cs)_ - List of fiber and handler pairs. Can be used for event handling. Can be used repeatedly. [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/FiberAndTaskPairListTests.cs#L93).
 * _[ISequentialTaskWaiter](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/ISequentialTaskWaiter.cs)_ - This allows you to await the execution timing of FiberAndTaskPairList. It can be created by `FiberAndTaskPairList.CreateWaiter()`. [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/SequentialTaskWaiterTests.cs#L11).
 * _[ISequentialHandlerWaiter{T}](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorks/Procedures/ISequentialHandlerWaiter.cs)_ - This allows you to wait for the execution timing of FiberAndHandlerPairList. It can be created by `FiberAndHandlerPairList<TMessage>.CreateWaiter()`. [Example](https://github.com/tosh-coding/AsyncFiberWorks/blob/main/src/AsyncFiberWorksTests/SequentialTaskWaiterTests.cs#L186).

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
