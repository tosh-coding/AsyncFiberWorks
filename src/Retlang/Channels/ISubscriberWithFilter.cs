namespace Retlang.Channels
{
    /// <summary>
    /// A subscriber with a filter that run in the producer thread.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISubscriberWithFilter<T> : IProducerThreadSubscriber<T>, IMessageFilter<T>
    {
    }
}
