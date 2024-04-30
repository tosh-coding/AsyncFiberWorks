using System;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// This channel is a publishing interface controlled by an acknowledgment.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TAck"></typeparam>
    public interface IAcknowledgementChannel<TMessage, TAck> : IAcknowledgementSubscriber<TMessage, TAck>, IAcknowledgementPublisher<TMessage, TAck>
    {
    }
}
