namespace Log2Console.Receiver
{
    /// <summary>
    ///     Delegate used when firing DebugMonitor.OnOutputDebug event
    /// </summary>
    public delegate void OnOutputDebugStringHandler(int pid, string text);
}