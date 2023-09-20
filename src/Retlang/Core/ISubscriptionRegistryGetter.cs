namespace Retlang.Core
{
    /// <summary>
    /// Provide ISubscriptionRegistry acquisition means.
    /// </summary>
    public interface ISubscriptionRegistryGetter
    {
        /// <summary>
        /// Instance for registration.
        /// </summary>
        ISubscriptionRegistry FallbackDisposer { get; }
    }
}
