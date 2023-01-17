using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace VagabondK.Interface.Abstractions
{
    public abstract class InterfaceHandler : INotifyPropertyChanged
    {
        private DateTime? timeStamp;
        private InterfacePoint point;
        private InterfaceMode mode;

        internal protected bool SetProperty<TProperty>(ref TProperty target, TProperty value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<TProperty>.Default.Equals(target, value))
            {
                target = value;
                RaisePropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        public abstract Task<bool> SendLocalValueAsync();
        public abstract bool SendLocalValue();

        public Task<bool> SendAsync<T>(T value) => SendAsync(value, null);
        public abstract Task<bool> SendAsync<T>(T value, DateTime? timeStamp);
        public bool Send<T>(T value) => Send(value, null);
        public abstract bool Send<T>(T value, DateTime? timeStamp);

        internal abstract void SetReceivedOtherTypeValue<T>(T value, DateTime? timeStamp);

        internal void RaiseErrorOccurred(Exception exception, ErrorDirection direction)
            => ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs(exception, direction));

        protected InterfaceHandler() { }
        protected InterfaceHandler(InterfaceMode mode)
        {
            Mode = mode;
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public InterfacePoint Point { get => point; internal set => SetProperty(ref point, value); }
        public InterfaceMode Mode { get => mode; set => SetProperty(ref mode, value); }
        public DateTime? TimeStamp { get => timeStamp; internal set => SetProperty(ref timeStamp, value); }
        public event ErrorOccurredEventHandler ErrorOccurred;
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
