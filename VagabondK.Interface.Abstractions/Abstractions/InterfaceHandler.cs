using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace VagabondK.Interface.Abstractions
{
    public abstract class InterfaceHandler : INotifyPropertyChanged
    {
        internal DateTime? timeStamp;
        private InterfacePoint point;
        private InterfaceMode mode;

        internal protected bool SetProperty<TProperty>(ref TProperty target, in TProperty value, [CallerMemberName] string propertyName = null)
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

        internal abstract void SetReceivedOtherTypeValue<T>(in T value, in DateTime? timeStamp);

        internal void RaiseErrorOccurred(Exception exception, ErrorDirection direction)
            => ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs(exception, direction));

        internal protected void SetTimeStamp(in DateTime? timeStamp) => SetProperty(ref this.timeStamp, timeStamp, nameof(TimeStamp));

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

        public Task<bool> SendAsync<T>(in T value) => SendAsync(value, null, null);
        public Task<bool> SendAsync<T>(in T value, DateTime? timeStamp) => SendAsync(value, timeStamp, null);
        public Task<bool> SendAsync<T>(in T value, CancellationToken? cancellationToken) => SendAsync(value, null, cancellationToken);
        public abstract Task<bool> SendAsync<T>(in T value, DateTime? timeStamp, CancellationToken? cancellationToken);

        public bool Send<T>(in T value) => Send(value, null);
        public abstract bool Send<T>(in T value, DateTime? timeStamp);
    }
}
