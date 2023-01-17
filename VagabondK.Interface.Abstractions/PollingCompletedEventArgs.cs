using System;

namespace VagabondK.Interface
{
    public class PollingCompletedEventArgs : EventArgs
    {
        public PollingCompletedEventArgs(bool succeed, Exception exception)
        {
            Succeed = succeed;
            Exception = exception;
        }

        public bool Succeed { get; }
        public Exception Exception { get; }
    }
}