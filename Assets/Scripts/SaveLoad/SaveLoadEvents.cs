namespace RTS.SaveLoad
{
    /// <summary>
    /// Event published when a save/load notification should be displayed to the user.
    /// </summary>
    public struct SaveLoadNotificationEvent
    {
        public string Message { get; }
        public bool IsError { get; }

        public SaveLoadNotificationEvent(string message, bool isError = false)
        {
            Message = message;
            IsError = isError;
        }
    }
}
