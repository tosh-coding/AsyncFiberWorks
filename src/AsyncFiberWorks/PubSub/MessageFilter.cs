namespace AsyncFiberWorks.PubSub
{
    /// <summary>
    /// Message filter delegate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="msg"></param>
    /// <returns>True to pass, false otherwise.</returns>
    public delegate bool MessageFilter<T>(T msg);
}