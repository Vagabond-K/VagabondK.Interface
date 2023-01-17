using System;

namespace VagabondK.Interface
{
    public class ErrorOccurredEventArgs : EventArgs
    {
        public ErrorOccurredEventArgs(Exception exception, ErrorDirection direction)
        {
            Exception = exception;
            Direction = direction;
        }

        public Exception Exception { get; }
        public ErrorDirection Direction { get; }
    }
}
