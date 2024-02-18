namespace AsyncFiberWorks.Channels
{
    /// <summary>
    /// Message filter delegate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="msg"></param>
    /// <returns>True to pass, false otherwise.</returns>
    public delegate bool Filter<T>(T msg);
}